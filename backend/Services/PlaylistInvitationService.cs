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
        PlaylistInvitationResponse Invite(string senderId, string playlistId, string receiverId);
        PlaylistResponse Accept(string currentUserId, string invitationId);
        void Decline(string currentUserId, string invitationId);
        void Cancel(string currentUserId, string invitationId);
        IEnumerable<PlaylistInvitationResponse> GetIncoming(string userId);
        IEnumerable<PlaylistInvitationResponse> GetOutgoing(string userId);
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


        public PlaylistInvitationResponse Invite(string senderId, string playlistId, string receiverId)
        {
            var (senderGuid, receiverGuid) = ParsePair(senderId, receiverId);
            var playlistGuid = ParseGuid(playlistId, "playlistId");

            if (senderGuid == receiverGuid)
                throw new AppException("You cannot send an invitation to yourself.");

            EnsureUserExists(receiverGuid, "The recipient does not exist.");

            // pull the playlist with the relationships we'll need
            var playlist = _context.Playlists
                .Include(p => p.SavedByAccounts)
                .FirstOrDefault(p => p.Id == playlistGuid)
                ?? throw new KeyNotFoundException("Playlist could not be found");

            // receiver is the owner? that's nonsense
            if (playlist.CreatedById == receiverGuid)
                throw new AppException("This user created the playlist; they already have it.");

            if (playlist.SavedByAccounts.Any(a => a.Id == receiverGuid))
                throw new AppException("This user has already saved this playlist.");

            // per-visibility auth check
            EnsureCanInvite(playlist, senderGuid);

            // block check
            var blockState = FindBlockBetween(senderGuid, receiverGuid);
            if (blockState == BlockDirection.SenderBlockedReceiver)
                throw new AppException("You have blocked this user. Unblock them first.");
            if (blockState == BlockDirection.ReceiverBlockedSender)
            {
                // synthetic response for idempotency
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

            // idempotency: existing pending invitation to this receiver
            // for this playlist, then return it as-is
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
            
            //load the playlist fresh with SavedByAccounts
            var playlist = _context.Playlists
                .Include(p => p.CreatedBy)
                .Include(p => p.SavedByAccounts)
                .Include(p => p.Songs)
                .FirstOrDefault(p => p.Id == row.PlaylistId)
                ?? throw new KeyNotFoundException("Playlist could not be found.");

            var receiver = _context.Accounts.FirstOrDefault(a => a.Id == currentGuid)
                ?? throw new KeyNotFoundException("Account not found.");

            if (!playlist.SavedByAccounts.Any(a => a.Id == currentGuid))
                playlist.SavedByAccounts.Add(receiver);

            // delete the invitation
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

            if (row.SenderId != currentGuid)
                throw new AppException("Only the sender can cancel an invitation.");

            _context.PlaylistInvitations.Remove(row);
            _context.SaveChanges();
        }


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

        //helper funcs

        private static void EnsureCanInvite(Playlist playlist, Guid senderGuid)
        {
            switch (playlist.Visibility)
            {
                case PlaylistVisibility.Private:
                    throw new AppException(
                        "Private playlists cannot be shared. Change the visibility first.");

                case PlaylistVisibility.Unlisted:
                    
                    if (playlist.CreatedById != senderGuid)
                        throw new AppException(
                            "Only the playlist's creator can share an unlisted playlist.");
                    return;

                case PlaylistVisibility.Public:
                    
                    return;

                default:
                    throw new AppException(
                        $"Unknown playlist visibility: {playlist.Visibility}.");
            }
        }

        private BlockDirection FindBlockBetween(Guid senderGuid, Guid receiverGuid)
        {
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