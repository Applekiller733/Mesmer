using AutoMapper;
using Microsoft.EntityFrameworkCore;
using SongAppApi.Authorization;
using SongAppApi.Entities;
using SongAppApi.Helpers;
using SongAppApi.Helpers.Enumerators;
using SongAppApi.Models.Playlist;
using SongAppApi.Models.Songs;

namespace SongAppApi.Services
{
    public interface IPlaylistService
    {
        PlaylistResponse Get(string id, string? currentUserId);
        PlaylistResponse GetInternal(string id);
        IEnumerable<PlaylistResponse> GetCreatedByAccount(string targetAccountId, string? currentUserId);
        IEnumerable<PlaylistResponse> GetSavedByAccount(string targetAccountId, string? currentUserId, bool isAdmin);

        /// <summary>
        /// Admin-only firehose. Use sparingly — returns every playlist in
        /// the database regardless of visibility. The controller gates
        /// this behind [Authorize(Role.Admin)].
        /// </summary>
        IEnumerable<PlaylistResponse> GetAll();

        PlaylistResponse Create(CreatePlaylistRequest request, Account account);
        PlaylistResponse Update(string id, UpdatePlaylistRequest request, string currentUserId, bool isAdmin);
        void Delete(string id, string currentUserId, bool isAdmin);
        PlaylistResponse Save(string playlistId, string currentUserId);
        void Unsave(string playlistId, string currentUserId);
        PlaylistResponse UpdateVisibility(string id, PlaylistVisibility visibility, string currentUserId, bool isAdmin);
    }

    public class PlaylistService : IPlaylistService
    {
        private readonly DataContext _context;
        private readonly IJwtUtils _jwtUtils;
        private readonly IMapper _mapper;

        public PlaylistService(DataContext context,
            IJwtUtils jwtUtils, IMapper mapper)
        {
            _context = context;
            _jwtUtils = jwtUtils;
            _mapper = mapper;
        }

        public PlaylistResponse Get(string id, string? currentUserId)
        {
            var playlist = getPlaylist(id);
            var viewerGuid = TryParseGuid(currentUserId);

            if (!CanView(playlist, viewerGuid))
            {
                // Deliberately throw the same exception as a missing row.
                // See the interface doc for rationale (info disclosure).
                throw new KeyNotFoundException("Playlist could not be found");
            }

            return _mapper.Map<PlaylistResponse>(playlist);
        }

        public PlaylistResponse GetInternal(string id)
        {
            var playlist = getPlaylist(id);
            return _mapper.Map<PlaylistResponse>(playlist);
        }

        public IEnumerable<PlaylistResponse> GetCreatedByAccount(string targetAccountId, string? currentUserId)
        {
            var playlists = getAllByAccount(targetAccountId);

            // Self-view: pass everything through. Other-view: hide
            // Unlisted and Private. The filtering is done in-memory after
            // the DB pull; the per-account playlist count is bounded and
            // small enough that an extra LINQ pass isn't worth a second
            // query path.
            if (currentUserId != null && currentUserId == targetAccountId)
                return _mapper.Map<List<PlaylistResponse>>(playlists);

            var visible = playlists
                .Where(p => p.Visibility == PlaylistVisibility.Public)
                .ToList();
            return _mapper.Map<List<PlaylistResponse>>(visible);
        }

        public IEnumerable<PlaylistResponse> GetSavedByAccount(string targetAccountId, string? currentUserId, bool isAdmin)
        {
            // Saved playlists are intrinsically personal — even for a
            // Public playlist, the fact that "user X saved playlist Y"
            // is metadata about user X. Don't expose it to others.
            if (!isAdmin && (currentUserId == null || currentUserId != targetAccountId))
                throw new AppException("You can only view your own saved playlists.");

            var playlists = getAllSavedByAccount(targetAccountId);
            return _mapper.Map<List<PlaylistResponse>>(playlists);
        }

        public IEnumerable<PlaylistResponse> GetAll()
        {
            // Caller (controller) is responsible for gating to Role.Admin.
            // We could re-check here but the service has no notion of role,
            // and double-gating bloats the interface.
            var playlists = getAll();
            return _mapper.Map<List<PlaylistResponse>>(playlists);
        }

        public PlaylistResponse Create(CreatePlaylistRequest request, Account account)
        {
            var playlist = _mapper.Map<Playlist>(request);
            playlist.CreatedBy = account;
            playlist.CreatedAt = DateTime.UtcNow;

            // Carry the requested visibility through; default to Private if
            // the client didn't specify. Mapping from CreatePlaylistRequest
            // sets this when the field is present, but AutoMapper will not
            // overwrite to default when null — be explicit.
            if (request.Visibility.HasValue)
                playlist.Visibility = request.Visibility.Value;
            else
                playlist.Visibility = PlaylistVisibility.Private;

            playlist.SavedByAccounts.Add(account);

            _context.Playlists.Add(playlist);
            _context.SaveChanges();

            return _mapper.Map<PlaylistResponse>(playlist);
        }

        public PlaylistResponse Update(string id, UpdatePlaylistRequest request, string currentUserId, bool isAdmin)
        {
            var playlist = getPlaylist(id);

            EnsureOwnerOrAdmin(playlist, currentUserId, isAdmin,
                "Only the playlist's owner can edit it.");

            _mapper.Map(request, playlist);
            playlist.UpdatedAt = DateTime.UtcNow;
            _context.Playlists.Update(playlist);
            _context.SaveChanges();

            return _mapper.Map<PlaylistResponse>(playlist);
        }

        public void Delete(string id, string currentUserId, bool isAdmin)
        {
            var playlist = getPlaylist(id);

            EnsureOwnerOrAdmin(playlist, currentUserId, isAdmin,
                "Only the playlist's owner can delete it.");

            _context.Playlists.Remove(playlist);
            _context.SaveChanges();
        }

        public PlaylistResponse Save(string playlistId, string currentUserId)
        {
            var playlist = getPlaylist(playlistId);
            var viewerGuid = TryParseGuid(currentUserId)
                ?? throw new AppException("Invalid user id.");

            // Reuse the same visibility predicate as Get — saving is just
            // "I want a persistent reference to a thing I can already see."
            // Throw the same not-found exception on failure so we don't
            // disclose private-playlist existence here either.
            if (!CanView(playlist, viewerGuid))
                throw new KeyNotFoundException("Playlist could not be found");

            // Idempotent: already saved is a successful no-op. Don't
            // bother hitting the DB for the join-table insert; just
            // return the current state.
            if (playlist.SavedByAccounts.Any(a => a.Id == viewerGuid))
                return _mapper.Map<PlaylistResponse>(playlist);

            var account = _context.Accounts.FirstOrDefault(a => a.Id == viewerGuid)
                ?? throw new KeyNotFoundException("Account not found.");

            playlist.SavedByAccounts.Add(account);

            // Direct-save also clears any pending invitation for this
            // (playlist, receiver) pair. The user has effectively
            // accepted, so the inbox row would otherwise sit there
            // duplicated for an already-saved playlist. FirstOrDefault
            // because the row may not exist (e.g., saving a Public
            // playlist no one invited them to).
            var pendingInvitation = _context.PlaylistInvitations
                .FirstOrDefault(i => i.PlaylistId == playlist.Id && i.ReceiverId == viewerGuid);
            if (pendingInvitation != null)
                _context.PlaylistInvitations.Remove(pendingInvitation);

            _context.SaveChanges();

            return _mapper.Map<PlaylistResponse>(playlist);
        }

        public void Unsave(string playlistId, string currentUserId)
        {
            var playlist = getPlaylist(playlistId);
            var viewerGuid = TryParseGuid(currentUserId)
                ?? throw new AppException("Invalid user id.");

            // Owner-unsave is intentionally rejected. Their library is
            // the canonical home of their creations; "I own this but
            // it's not in my library" is a confusing state that should
            // be Delete instead.
            if (playlist.CreatedById == viewerGuid)
                throw new AppException(
                    "You can't unsave a playlist you created. Delete it instead.");

            var account = playlist.SavedByAccounts.FirstOrDefault(a => a.Id == viewerGuid);
            if (account == null)
                throw new KeyNotFoundException("This playlist is not in your library.");

            playlist.SavedByAccounts.Remove(account);
            _context.SaveChanges();
        }

        public PlaylistResponse UpdateVisibility(
            string id, PlaylistVisibility visibility, string currentUserId, bool isAdmin)
        {
            var playlist = getPlaylist(id);

            EnsureOwnerOrAdmin(playlist, currentUserId, isAdmin,
                "Only the playlist's owner can change its visibility.");

            // Defensive: reject an unknown enum value rather than letting
            // it land in the database. Enum.IsDefined catches the case
            // where the JSON deserialiser accepted an out-of-range int.
            if (!Enum.IsDefined(typeof(PlaylistVisibility), visibility))
                throw new AppException($"Invalid visibility value: {visibility}.");

            // No-op fast path. Saves a write and avoids spuriously
            // cancelling invitations if a UI accidentally re-PATCHes
            // the same value.
            if (playlist.Visibility == visibility)
                return _mapper.Map<PlaylistResponse>(playlist);

            // Side effect: transitioning TO Private invalidates pending
            // invitations to this playlist. The playlist is no longer
            // shareable in that state, so the inbox rows would be
            // stale (and accepting one would land the receiver in a
            // weird state where they have a Private playlist they
            // technically can see).
            //
            // Transitions between Public and Unlisted leave invitations
            // alone — both states still allow sharing in some form, and
            // the invitations were valid at the time they were sent.
            if (visibility == PlaylistVisibility.Private)
            {
                var stalePendingInvitations = _context.PlaylistInvitations
                    .Where(i => i.PlaylistId == playlist.Id)
                    .ToList();
                if (stalePendingInvitations.Count > 0)
                    _context.PlaylistInvitations.RemoveRange(stalePendingInvitations);
            }

            playlist.Visibility = visibility;
            playlist.UpdatedAt = DateTime.UtcNow;
            _context.SaveChanges();

            return _mapper.Map<PlaylistResponse>(playlist);
        }

        /// <summary>
        /// Owner-or-admin gate. The owner-only/admin distinction matches
        /// the controller-level check the codebase already uses for
        /// account update/delete (AccountsController.Delete).
        /// </summary>
        private static void EnsureOwnerOrAdmin(
            Playlist playlist, string currentUserId, bool isAdmin, string forbiddenMessage)
        {
            if (isAdmin) return;

            if (!Guid.TryParse(currentUserId, out var uid))
                throw new AppException("Invalid user id.");

            if (playlist.CreatedById != uid)
                throw new AppException(forbiddenMessage);
        }

        // ---- Visibility helper ----

        /// <summary>
        /// Central authorization predicate. Anything that reads a single
        /// playlist on behalf of a user routes through here. Keep the
        /// logic in one place so the rules don't drift between Get,
        /// future share endpoints, and the recommender's safe-mode read.
        /// </summary>
        private bool CanView(Playlist playlist, Guid? userId)
        {
            // Public is universally visible to authenticated AND
            // unauthenticated callers. (Anonymous routes don't exist yet
            // but this leaves the door open.)
            if (playlist.Visibility == PlaylistVisibility.Public)
                return true;

            // Below this point we need to know who's asking.
            if (userId == null)
                return false;

            var uid = userId.Value;

            // Owner always sees their own playlists.
            if (playlist.CreatedById == uid)
                return true;

            // A user who has the playlist in their saved library has
            // legitimate access — whether they got it via accepting an
            // invitation or by saving while it was Public and the owner
            // later flipped it Private. We deliberately don't strip past
            // access on visibility change (per the design decision).
            if (playlist.SavedByAccounts.Any(a => a.Id == uid))
                return true;

            // Unlisted: also allow users with a pending invitation to
            // this playlist. They need to be able to preview before
            // accepting; without this, an invitee would see "Playlist
            // could not be found" when clicking their own inbox entry.
            //
            // For Private, no invitations exist (sharing requires
            // Unlisted or Public), so this branch is unreachable for
            // Private playlists in practice — guarded explicitly anyway
            // so a future bug that creates a stray invitation row
            // against a Private playlist can't accidentally widen
            // visibility.
            if (playlist.Visibility == PlaylistVisibility.Unlisted &&
                _context.PlaylistInvitations.Any(i =>
                    i.PlaylistId == playlist.Id && i.ReceiverId == uid))
            {
                return true;
            }

            return false;
        }

        private static Guid? TryParseGuid(string? value)
        {
            if (string.IsNullOrEmpty(value)) return null;
            return Guid.TryParse(value, out var g) ? g : (Guid?)null;
        }

        // ---- Existing entity-level helpers (unchanged) ----

        public Playlist getPlaylist(string id)
        {
            var playlist = _context.Playlists
                .Include(p => p.CreatedBy)
                .Include(p => p.SavedByAccounts)
                .Include(p => p.Songs)
                .FirstOrDefault(p => p.Id.ToString() == id);
            if (playlist == null)
                throw new KeyNotFoundException("Playlist could not be found");
            return playlist;
        }

        public List<Playlist> getAll()
        {
            var playlists = _context.Playlists
                .Include(p => p.CreatedBy)
                .Include(p => p.SavedByAccounts)
                .Include(p => p.Songs)
                .ToList();
            if (playlists == null)
                throw new KeyNotFoundException("Playlists could not be found");
            return playlists;
        }

        public List<Playlist> getAllByAccount(string accountid)
        {
            var playlists = _context.Playlists
                .Include(p => p.CreatedBy)
                .Include(p => p.SavedByAccounts)
                .Include(p => p.Songs)
                .Where(p => p.CreatedBy.Id.ToString() == accountid)
                .ToList();
            if (playlists == null)
                throw new KeyNotFoundException("Playlists created by account could not be found");
            return playlists;
        }

        public List<Playlist> getAllSavedByAccount(string accountid)
        {
            var playlists = _context.Playlists
                    .Include(p => p.CreatedBy)
                    .Include(p => p.SavedByAccounts)
                    .Include(p => p.Songs)
                .Where(p => p.SavedByAccounts.Any(a => a.Id.ToString() == accountid))
                .ToList();
            if (playlists == null)
                throw new KeyNotFoundException("Playlists saved by account could not be found");
            return playlists;
        }
    }
}