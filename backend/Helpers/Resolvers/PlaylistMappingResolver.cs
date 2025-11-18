using AutoMapper;
using SongAppApi.Entities;
using SongAppApi.Models.Playlist;

namespace SongAppApi.Helpers.Resolvers
{
    public class CreatePlaylistSongResolver : IValueResolver<CreatePlaylistRequest, Playlist, List<Song>>
    {
        private readonly DataContext _context;

        public CreatePlaylistSongResolver(DataContext context)
        {
            _context = context;
        }

        public List<Song> Resolve(CreatePlaylistRequest source, Playlist destination, List<Song> destMember, ResolutionContext context)
        {
            return _context.Songs.Where(s => source.SongIds.Contains(s.Id)).ToList();
        }
    }

    public class UpdatePlaylistSongResolver : IValueResolver<UpdatePlaylistRequest, Playlist, List<Song>>
    {
        private readonly DataContext _context;

        public UpdatePlaylistSongResolver(DataContext context)
        {
            _context = context;
        }

        public List<Song> Resolve(UpdatePlaylistRequest source, Playlist destination, List<Song> destMember, ResolutionContext context)
        {
            return _context.Songs.Where(s => source.SongIds.Contains(s.Id)).ToList();
        }
    }
}
