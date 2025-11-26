namespace SongAppApi.Models.Playlist
{
    public class UpdatePlaylistRequest
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public List<string> SongIds { get; set; }
    }
}
