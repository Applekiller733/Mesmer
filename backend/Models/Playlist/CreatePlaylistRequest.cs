using SongAppApi.Entities;
using SongAppApi.Models.Songs;

namespace SongAppApi.Models.Playlist
{
    public class CreatePlaylistRequest
    {
        public string Name { get; set; }
        public List<int> SongIds { get; set; }
    }
}
