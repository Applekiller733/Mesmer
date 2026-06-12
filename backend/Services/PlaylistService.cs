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
                //throw not found rather than forbid
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

            if (currentUserId != null && currentUserId == targetAccountId)
                return _mapper.Map<List<PlaylistResponse>>(playlists);

            var visible = playlists
                .Where(p => p.Visibility == PlaylistVisibility.Public)
                .ToList();
            return _mapper.Map<List<PlaylistResponse>>(visible);
        }

        public IEnumerable<PlaylistResponse> GetSavedByAccount(string targetAccountId, string? currentUserId, bool isAdmin)
        {
            if (!isAdmin && (currentUserId == null || currentUserId != targetAccountId))
                throw new AppException("You can only view your own saved playlists.");

            var playlists = getAllSavedByAccount(targetAccountId);
            return _mapper.Map<List<PlaylistResponse>>(playlists);
        }

        public IEnumerable<PlaylistResponse> GetAll()
        {
            var playlists = getAll();
            return _mapper.Map<List<PlaylistResponse>>(playlists);
        }

        public PlaylistResponse Create(CreatePlaylistRequest request, Account account)
        {
            var playlist = _mapper.Map<Playlist>(request);
            playlist.CreatedBy = account;
            playlist.CreatedAt = DateTime.UtcNow;

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

            if (!CanView(playlist, viewerGuid))
                throw new KeyNotFoundException("Playlist could not be found");

            // idempotency
            if (playlist.SavedByAccounts.Any(a => a.Id == viewerGuid))
                return _mapper.Map<PlaylistResponse>(playlist);

            var account = _context.Accounts.FirstOrDefault(a => a.Id == viewerGuid)
                ?? throw new KeyNotFoundException("Account not found.");

            playlist.SavedByAccounts.Add(account);

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

            // owner cant unsave, only delete
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

            //reject unknown enum
            if (!Enum.IsDefined(typeof(PlaylistVisibility), visibility))
                throw new AppException($"Invalid visibility value: {visibility}.");

            //no op if vis is correct
            if (playlist.Visibility == visibility)
                return _mapper.Map<PlaylistResponse>(playlist);

            // if switching to private, delete any pending invitations
            // since they can't be accepted anymore and would be confusing
            // to leave hanging around

            // don't worry about existing accepted
            // invitations or saved library entries,
            // those users keep access until they lose it by unsaving or the owner
            // deleting the playlist. 
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

        private static void EnsureOwnerOrAdmin(
            Playlist playlist, string currentUserId, bool isAdmin, string forbiddenMessage)
        {
            if (isAdmin) return;

            if (!Guid.TryParse(currentUserId, out var uid))
                throw new AppException("Invalid user id.");

            if (playlist.CreatedById != uid)
                throw new AppException(forbiddenMessage);
        }

        // vis helper
        private bool CanView(Playlist playlist, Guid? userId)
        {
            if (playlist.Visibility == PlaylistVisibility.Public)
                return true;

            if (userId == null)
                return false;

            var uid = userId.Value;

            if (playlist.CreatedById == uid)
                return true;

            if (playlist.SavedByAccounts.Any(a => a.Id == uid))
                return true;

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