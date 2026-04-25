using AutoMapper;
using Microsoft.EntityFrameworkCore;
using SongAppApi.Authorization;
using SongAppApi.Entities;
using SongAppApi.Helpers;
using SongAppApi.Helpers.Enumerators;
using SongAppApi.Models.Songs;

namespace SongAppApi.Services
{
    public interface ISongService
    {
        SongResponse Get(string id);
        IEnumerable<SongResponse> GetAll();
        IEnumerable<string> GetAllIds();
        SongResponse Create(CreateSongRequest request, Account account);
        void Delete(string id);
        UpvotesResponse FlipLike(string id, Account account);

        // New: needed by the audio streaming endpoint to find the on-disk file.
        Entities.File? GetSoundFile(string songId);
    }

    public class SongService : ISongService
    {
        private readonly DataContext _context;
        private readonly IJwtUtils _jwtUtils;
        private readonly IMapper _mapper;
        private readonly IFileService _fileService;

        public SongService(DataContext context,
            IJwtUtils jwtUtils, IMapper mapper, IFileService fileService)
        {
            _context = context;
            _jwtUtils = jwtUtils;
            _mapper = mapper;
            _fileService = fileService;
        }

        public SongResponse Get(string id)
        {
            var song = getSong(id);
            return _mapper.Map<SongResponse>(song);
        }

        public IEnumerable<SongResponse> GetAll()
        {
            var songs = _context.Songs;
            return _mapper.Map<List<SongResponse>>(songs);
        }

        public IEnumerable<string> GetAllIds()
        {
            return _context.Songs.Select(s => s.Id.ToString());
        }

        public SongResponse Create(CreateSongRequest request, Account creator)
        {
            // Map metadata fields only. SoundFile is intentionally not in AutoMapper.
            var song = _mapper.Map<Song>(request);
            song.CreatedBy = creator;
            song.CreatedAt = DateTime.UtcNow;
            song.Upvotes = 0;

            // If an audio file was uploaded, persist it and link it.
            // The SoundUrl field on the response will be populated post-save
            // by the controller (which knows the route), or you can build it
            // here from a known route prefix — we leave it null and let the
            // mapper / controller fill it in.
            if (request.SoundFile != null && request.SoundFile.Length > 0)
            {
                var file = _fileService.CreateFromFormFile(
                    request.SoundFile,
                    subfolderpath: Path.Combine("Songs", "Audio"),
                    category: FileCategory.Audio);

                song.SoundId = file.Id;
                // Clear any external URL the client also sent — the uploaded
                // file is the source of truth and we don't want both set.
                song.SoundUrl = null;
            }

            _context.Songs.Add(song);
            _context.SaveChanges();

            return _mapper.Map<SongResponse>(song);
        }

        public void Delete(string id)
        {
            var song = getSong(id);
            _context.Songs.Remove(song);
            _context.SaveChanges();
        }

        public UpvotesResponse FlipLike(string id, Account account)
        {
            var song = getSong(id);
            if (song.LikedByAccounts.Any(a => a.Id == account.Id))
            {
                song.LikedByAccounts.Remove(account);
            }
            else
            {
                song.LikedByAccounts.Add(account);
            }
            song.Upvotes = song.LikedByAccounts.Count;
            _context.SaveChanges();
            return _mapper.Map<UpvotesResponse>(song);
        }

        public Entities.File? GetSoundFile(string songId)
        {
            var song = _context.Songs
                .Include(s => s.Sound)
                .FirstOrDefault(s => s.Id == Guid.Parse(songId));

            return song?.Sound;
        }

        private Song getSong(string id)
        {
            var song = _context.Songs
                .Include(s => s.LikedByAccounts)
                .Include(s => s.CreatedBy)
                .FirstOrDefault(s => s.Id == Guid.Parse(id));
            if (song == null) throw new KeyNotFoundException("Song not found");
            return song;
        }
    }
}