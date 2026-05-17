using AutoMapper;
using Microsoft.EntityFrameworkCore;
using SongAppApi.Entities;
using SongAppApi.Helpers;
using SongAppApi.Helpers.Enumerators;
using SongAppApi.Models.Playlist;
using SongAppApi.Models;
using SongAppApi.Models.PlaylistInvitations;

namespace SongAppApi.Services
{
    public interface IPlaylistInvitationService
    {
        /// <summary>
        /// Send a playlist invitation. Authority depends on the playlist's
        /// visibility:
        ///   - Private: no one can invite (the privacy is the point;
        ///     change visibility first).
        ///   - Unlisted: only the playlist's owner.
        ///   - Public: anyone who can see the playlist.
        ///
        /// Idempotent on (playlist, receiver): re-sending while a row
        /// already exists returns the existing row rather than erroring
        /// on the unique constraint. Shadow-blocks (receiver blocked
        /// sender) return a synthetic response that looks successful
        /// without inserting anything.
        ///
        /// Throws KeyNotFoundException if the playlist or receiver
        /// doesn't exist, AppException for authority / state issues.
        /// </summary>
        PlaylistInvitationResponse Invite(string senderId, string playlistId, string receiverId);

        /// <summary>
        /// Accept an incoming invitation. Side effects: the playlist is
        /// added to the receiver's SavedByAccounts list AND the
        /// invitation row is deleted (acceptance is represented by
        /// membership in SavedByAccounts, not by a state on this row).
        ///
        /// Returns the playlist the receiver just accepted so the
        /// frontend can navigate to it without a follow-up fetch.
        /// </summary>
        PlaylistResponse Accept(string currentUserId, string invitationId);

        /// <summary>
        /// Decline an incoming invitation. Removes the row, no other
        /// side effects. Only the receiver can decline.
        /// </summary>
        void Decline(string currentUserId, string invitationId);

        /// <summary>
        /// Sender cancels their own outgoing invitation before it's been
        /// accepted/declined. Different from Decline only in who's
        /// authorised (sender vs receiver) — the row deletion is the same.
        /// </summary>
        void Cancel(string currentUserId, string invitationId);

        IEnumerable<PlaylistInvitationResponse> GetIncoming(string userId);
        IEnumerable<PlaylistInvitationResponse> GetOutgoing(string userId);

        /// <summary>
        /// Count of pending incoming invitations. Cheap; used by the
        /// navbar badge to avoid pulling the full inbox each poll.
        /// </summary>
        int CountIncoming(string userId);
    }

    public class PlaylistInvitationService : IPlaylistInvitationService
    {
        private readonly DataContext _context;
        private readonly IMapper _mapper;

        public PlaylistInvitationService(DataContext context, IMapper mapper)
        {
            _context = context;
            _mapper = mapper;
        }

        // -------------------- State changes --------------------

        public PlaylistInvitationResponse Invite(string senderId, string playlistId, string receiverId)
        {
            var (senderGuid, receiverGuid) = ParsePair(senderId, receiverId);
            var playlistGuid = ParseGuid(playlistId, "playlistId");

            if (senderGuid == receiverGuid)
                throw new AppException("You cannot send an invitation to yourself.");

            EnsureUserExists(receiverGuid, "The recipient does not exist.");

            // Pull the playlist with the relationships we'll need for the
            // authority and "already saved" checks.
            var playlist = _context.Playlists
                .Include(p => p.SavedByAccounts)
                .FirstOrDefault(p => p.Id == playlistGuid)
                ?? throw new KeyNotFoundException("Playlist could not be found");

            // Receiver is the owner? That's nonsense — they already have it.
            if (playlist.CreatedById == receiverGuid)
                throw new AppException("This user created the playlist; they already have it.");

            // Receiver already saved it? Don't pile up redundant invitations.
            if (playlist.SavedByAccounts.Any(a => a.Id == receiverGuid))
                throw new AppException("This user has already saved this playlist.");

            // Per-visibility authority check.
            EnsureCanInvite(playlist, senderGuid);

            // Block check. Mirrors the FriendshipService shadow-block
            // policy: if the receiver blocked the sender, pretend success
            // (don't reveal the block). If the sender blocked the
            // receiver, that's a clear user-facing error.
            var blockState = FindBlockBetween(senderGuid, receiverGuid);
            if (blockState == BlockDirection.SenderBlockedReceiver)
                throw new AppException("You have blocked this user. Unblock them first.");
            if (blockState == BlockDirection.ReceiverBlockedSender)
            {
                // Synthetic response — looks successful from the
                // sender's perspective; nothing actually inserted.
                return new PlaylistInvitationResponse
                {
                    PlaylistId = playlistId,
                    PlaylistName = playlist.Name,
                    PlaylistVisibility = playlist.Visibility,
                    SenderId = senderId,
                    ReceiverId = receiverId,
                    CreatedAt = DateTime.UtcNow,
                };
            }

            // Idempotency: existing pending invitation to this receiver
            // for this playlist? Return it as-is. The (PlaylistId,
            // ReceiverId) unique index makes the lookup O(1).
            var existing = _context.PlaylistInvitations
                .Include(i => i.Playlist)
                .Include(i => i.Sender)
                .Include(i => i.Receiver)
                .FirstOrDefault(i => i.PlaylistId == playlistGuid && i.ReceiverId == receiverGuid);
            if (existing != null)
                return _mapper.Map<PlaylistInvitationResponse>(existing);

            var invitation = new PlaylistInvitation
            {
                PlaylistId = playlistGuid,
                SenderId = senderGuid,
                ReceiverId = receiverGuid,
                CreatedAt = DateTime.UtcNow,
            };
            _context.PlaylistInvitations.Add(invitation);
            _context.SaveChanges();

            // Re-fetch with includes so the mapper has the navigation
            // properties it needs. Cheaper than wiring them by hand on
            // the freshly-inserted entity (the playlist + sender are
            // already in our local tracking from the lookups above,
            // but the receiver isn't).
            var hydrated = _context.PlaylistInvitations
                .Include(i => i.Playlist)
                .Include(i => i.Sender)
                .Include(i => i.Receiver)
                .First(i => i.Id == invitation.Id);
            return _mapper.Map<PlaylistInvitationResponse>(hydrated);
        }

        public PlaylistResponse Accept(string currentUserId, string invitationId)
        {
            var currentGuid = ParseGuid(currentUserId, "currentUserId");
            var iGuid = ParseGuid(invitationId, "invitationId");

            var row = _context.PlaylistInvitations
                .FirstOrDefault(i => i.Id == iGuid)
                ?? throw new KeyNotFoundException("Invitation not found.");

            if (row.ReceiverId != currentGuid)
                throw new AppException("Only the invitation's recipient can accept it.");

            // Load the playlist fresh with SavedByAccounts. We need to
            // mutate the collection, and the row.Playlist navigation
            // wasn't necessarily eagerly loaded.
            var playlist = _context.Playlists
                .Include(p => p.CreatedBy)
                .Include(p => p.SavedByAccounts)
                .Include(p => p.Songs)
                .FirstOrDefault(p => p.Id == row.PlaylistId)
                ?? throw new KeyNotFoundException("Playlist could not be found.");

            var receiver = _context.Accounts.FirstOrDefault(a => a.Id == currentGuid)
                ?? throw new KeyNotFoundException("Account not found.");

            // Defensive: if somehow the user already has it saved
            // (concurrent save via /playlists/{id}/save while the
            // invitation was pending), just drop the row and skip the
            // duplicate Add. Otherwise EF would try to insert a dup
            // into the join table.
            if (!playlist.SavedByAccounts.Any(a => a.Id == currentGuid))
                playlist.SavedByAccounts.Add(receiver);

            // Delete the invitation. Acceptance is now represented by
            // membership in SavedByAccounts — no need to keep a row.
            _context.PlaylistInvitations.Remove(row);
            _context.SaveChanges();

            return _mapper.Map<PlaylistResponse>(playlist);
        }

        public void Decline(string currentUserId, string invitationId)
        {
            var currentGuid = ParseGuid(currentUserId, "currentUserId");
            var iGuid = ParseGuid(invitationId, "invitationId");

            var row = _context.PlaylistInvitations
                .FirstOrDefault(i => i.Id == iGuid)
                ?? throw new KeyNotFoundException("Invitation not found.");

            if (row.ReceiverId != currentGuid)
                throw new AppException("Only the invitation's recipient can decline it.");

            _context.PlaylistInvitations.Remove(row);
            _context.SaveChanges();
        }

        public void Cancel(string currentUserId, string invitationId)
        {
            var currentGuid = ParseGuid(currentUserId, "currentUserId");
            var iGuid = ParseGuid(invitationId, "invitationId");

            var row = _context.PlaylistInvitations
                .FirstOrDefault(i => i.Id == iGuid)
                ?? throw new KeyNotFoundException("Invitation not found.");

            // Different gate from Decline — the sender, not the receiver,
            // is the authorised party. This is "I want to take back what
            // I shared", not "I don't want this thing offered to me".
            if (row.SenderId != currentGuid)
                throw new AppException("Only the sender can cancel an invitation.");

            _context.PlaylistInvitations.Remove(row);
            _context.SaveChanges();
        }

        // -------------------- Queries --------------------

        public IEnumerable<PlaylistInvitationResponse> GetIncoming(string userId)
        {
            var guid = ParseGuid(userId, "userId");

            var rows = _context.PlaylistInvitations
                .Include(i => i.Playlist)
                .Include(i => i.Sender)
                .Include(i => i.Receiver)
                .Where(i => i.ReceiverId == guid)
                .OrderByDescending(i => i.CreatedAt)
                .ToList();

            return _mapper.Map<IEnumerable<PlaylistInvitationResponse>>(rows);
        }

        public IEnumerable<PlaylistInvitationResponse> GetOutgoing(string userId)
        {
            var guid = ParseGuid(userId, "userId");

            var rows = _context.PlaylistInvitations
                .Include(i => i.Playlist)
                .Include(i => i.Sender)
                .Include(i => i.Receiver)
                .Where(i => i.SenderId == guid)
                .OrderByDescending(i => i.CreatedAt)
                .ToList();

            return _mapper.Map<IEnumerable<PlaylistInvitationResponse>>(rows);
        }

        public int CountIncoming(string userId)
        {
            var guid = ParseGuid(userId, "userId");
            return _context.PlaylistInvitations.Count(i => i.ReceiverId == guid);
        }

        // -------------------- Helpers --------------------

        /// <summary>
        /// Per-visibility authority check for sending an invitation.
        /// Centralised so the rules don't drift across the codebase.
        /// </summary>
        private static void EnsureCanInvite(Playlist playlist, Guid senderGuid)
        {
            switch (playlist.Visibility)
            {
                case PlaylistVisibility.Private:
                    // No one can invite for a Private playlist — not even
                    // the owner. They have to make it Unlisted or Public
                    // first. This is more discoverable than silently
                    // letting the owner invite (which would imply Private
                    // is just "default off"; it isn't — it's "off, period").
                    throw new AppException(
                        "Private playlists cannot be shared. Change the visibility first.");

                case PlaylistVisibility.Unlisted:
                    // Only the owner. The whole point of Unlisted is "the
                    // owner is the sole distributor". A user who saved it
                    // via an earlier invitation cannot fan it out further.
                    if (playlist.CreatedById != senderGuid)
                        throw new AppException(
                            "Only the playlist's creator can share an unlisted playlist.");
                    return;

                case PlaylistVisibility.Public:
                    // Anyone can share. We've already established the
                    // sender can see the playlist (it's Public).
                    return;

                default:
                    // Defensive: a future visibility added to the enum
                    // should explicitly opt in to one of these branches
                    // rather than silently inheriting Public-style "any
                    // user may invite" semantics.
                    throw new AppException(
                        $"Unknown playlist visibility: {playlist.Visibility}.");
            }
        }

        /// <summary>
        /// Direction of the most-recent Blocked relationship between two
        /// users, if any. Used to mirror the FriendshipService shadow-block
        /// policy: receiver-blocks-sender invitations silently no-op so
        /// the sender can't probe for the block.
        /// </summary>
        private BlockDirection FindBlockBetween(Guid senderGuid, Guid receiverGuid)
        {
            // Sender blocked Receiver: there's a row with SenderId=sender,
            // ReceiverId=receiver, Status=Blocked.
            var senderBlockedReceiver = _context.Friendships.Any(f =>
                f.SenderId == senderGuid &&
                f.ReceiverId == receiverGuid &&
                f.Status == FriendshipStatus.Blocked);
            if (senderBlockedReceiver) return BlockDirection.SenderBlockedReceiver;

            var receiverBlockedSender = _context.Friendships.Any(f =>
                f.SenderId == receiverGuid &&
                f.ReceiverId == senderGuid &&
                f.Status == FriendshipStatus.Blocked);
            if (receiverBlockedSender) return BlockDirection.ReceiverBlockedSender;

            return BlockDirection.None;
        }

        private enum BlockDirection { None, SenderBlockedReceiver, ReceiverBlockedSender }

        // Argument parsers / existence checks. Lifted from FriendshipService
        // verbatim — same conventions, same error messages so the API
        // surface is consistent.

        private static (Guid, Guid) ParsePair(string first, string second)
        {
            return (ParseGuid(first, "first"), ParseGuid(second, "second"));
        }

        private static Guid ParseGuid(string value, string paramName)
        {
            if (!Guid.TryParse(value, out var guid))
                throw new AppException($"{paramName} is not a valid id.");
            return guid;
        }

        private void EnsureUserExists(Guid id, string notFoundMessage)
        {
            if (!_context.Accounts.Any(a => a.Id == id))
                throw new KeyNotFoundException(notFoundMessage);
        }
    }
}