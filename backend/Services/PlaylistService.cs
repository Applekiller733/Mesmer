using AutoMapper;
using Microsoft.EntityFrameworkCore;
using SongAppApi.Authorization;
using SongAppApi.Entities;
using SongAppApi.Helpers;
using SongAppApi.Models.Playlist;
using SongAppApi.Models.Songs;

namespace SongAppApi.Services
{
    public interface IPlaylistService
    {
        PlaylistResponse Get(int id);
        IEnumerable<PlaylistResponse> GetCreatedByAccount(int accountid);
        IEnumerable<PlaylistResponse> GetSavedByAccount(int accountid);
        IEnumerable<PlaylistResponse> GetAll();
        PlaylistResponse Create(CreatePlaylistRequest request, Account account);
        PlaylistResponse Update(int id, UpdatePlaylistRequest request);
        void Delete(int id);
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

        public PlaylistResponse Get(int id)
        {
            var playlist = getPlaylist(id);
            return _mapper.Map<PlaylistResponse>(playlist);
        }

        public IEnumerable<PlaylistResponse> GetCreatedByAccount(int accountid)
        {
            var playlists = getAllByAccount(accountid);
            return _mapper.Map<List<PlaylistResponse>>(playlists);
        }

        public IEnumerable<PlaylistResponse> GetSavedByAccount(int accountid)
        {
            var playlists = getAllSavedByAccount(accountid);
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
            playlist.SavedByAccounts.Add(account);

            _context.Playlists.Add(playlist);
            _context.SaveChanges();

            return _mapper.Map<PlaylistResponse>(playlist);
        }

        public PlaylistResponse Update(int id, UpdatePlaylistRequest request)
        {
            var playlist = getPlaylist(id);

            _mapper.Map(request, playlist);
            playlist.UpdatedAt = DateTime.UtcNow;
            _context.Playlists.Update(playlist);
            _context.SaveChanges();

            return _mapper.Map<PlaylistResponse>(playlist);
        }

        public void Delete(int id)
        {
            var playlist = getPlaylist(id);
            _context.Playlists.Remove(playlist);
            _context.SaveChanges();
        }

        //helper

        public Playlist getPlaylist(int id)
        {
            var playlist = _context.Playlists
                .Include(p => p.CreatedBy)
                .Include(p => p.SavedByAccounts)
                .Include(p => p.Songs)
                .FirstOrDefault(p => p.Id == id);
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

        public List<Playlist> getAllByAccount(int accountid)
        {
            var playlists = _context.Playlists
                .Include(p => p.CreatedBy)
                .Include(p => p.SavedByAccounts)
                .Include(p => p.Songs)
                .Where(p => p.CreatedBy.Id == accountid)
                .ToList();
            if (playlists == null)
                throw new KeyNotFoundException("Playlists created by account could not be found");
            return playlists;
        }

        public List<Playlist> getAllSavedByAccount(int accountid)
        {
            var playlists = _context.Playlists
                    .Include(p => p.CreatedBy)
                    .Include(p => p.SavedByAccounts)
                    .Include(p => p.Songs)
                .Where(p => p.SavedByAccounts.Any(a => a.Id == accountid))
                .ToList();
            if (playlists == null)
                throw new KeyNotFoundException("Playlists saved by account could not be found");
            return playlists;
        }
    }
}
