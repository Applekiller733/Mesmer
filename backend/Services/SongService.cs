using AutoMapper;
using Microsoft.EntityFrameworkCore;
using SongAppApi.Authorization;
using SongAppApi.Entities;
using SongAppApi.Helpers;
using SongAppApi.Models.Songs;

namespace SongAppApi.Services
{
    public interface ISongService
    {
        SongResponse Get(int id);
        IEnumerable<SongResponse> GetAll();
        IEnumerable<int> GetAllIds();
        SongResponse Create(CreateSongRequest request, Account account);
        //todo add update?
        //SongResponse Update(UpdateSongRequest request); 
        //??
        void Delete(int id);
        UpvotesResponse FlipLike(int id, Account account);
        //void Unlike(int id, Account account);
    }
    public class SongService : ISongService
    {
        private readonly DataContext _context;
        private readonly IJwtUtils _jwtUtils;
        private readonly IMapper _mapper;

        public SongService(DataContext context,
            IJwtUtils jwtUtils, IMapper mapper)
        {
            _context = context;
            _jwtUtils = jwtUtils;
            _mapper = mapper;
        }

        public SongResponse Get(int id)
        {
            var song = getSong(id);
            return _mapper.Map<SongResponse>(song);
        }

        public IEnumerable<SongResponse> GetAll()
        {
            var songs = _context.Songs;
            return _mapper.Map<List<SongResponse>>(songs);
        }

        public IEnumerable<int> GetAllIds()
        {
            return _context.Songs.Select(s => s.Id);
        }

        public SongResponse Create(CreateSongRequest request, Account creator)
        {
            var song = _mapper.Map<Song>(request);
            song.CreatedBy = creator;
            song.CreatedAt = DateTime.UtcNow;
            song.Upvotes = 0;

            _context.Songs.Add(song);
            _context.SaveChanges();

            return _mapper.Map<SongResponse>(song);
        }

        public void Delete(int id)
        {
            var song = getSong(id);
            _context.Songs.Remove(song);
            _context.SaveChanges();
        }

        public UpvotesResponse FlipLike(int id, Account account)
        {
            var song = getSong(id);
            if (song.LikedByAccounts.Any(a => a.Id == account.Id))
            {
                //Console.WriteLine("Inside the remove statement");
                song.LikedByAccounts.Remove(account);
            }
            else
            {
                //Console.WriteLine("Inside the Add Statement");
                song.LikedByAccounts.Add(account);
            }
            Console.WriteLine(song.LikedByAccounts.ToArray().ToString());
            song.Upvotes = song.LikedByAccounts.Count;
            _context.Songs.Update(song);
            _context.SaveChanges();
            return _mapper.Map<UpvotesResponse>(song);
        }

        //public void Unlike(int id, Account account)
        //{
        //    var song = getSong(id);
        //    if (song.LikedByAccounts.FirstOrDefault(a => a.Id == account.Id) == null)
        //        return;
        //song.LikedByAccounts.Remove(account);
        //    song.Upvotes = song.LikedByAccounts.Count;
        //    _context.Songs.Update(song);
        //    _context.SaveChanges();
        //}

        //helperss

        public Song getSong(int id)
        {
            var song = _context.Songs
                .Include(s => s.CreatedBy)
                .Include(s => s.LikedByAccounts)
                .Include(s => s.Image)
                .Include(s => s.Sound)
                .Include(s => s.Video)
                .FirstOrDefault(s => s.Id == id);

            if (song == null)
                throw new KeyNotFoundException("Song could not be found");
            return song;
        }
    }
}
