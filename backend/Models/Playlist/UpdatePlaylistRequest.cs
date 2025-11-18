namespace SongAppApi.Models.Playlist
{
    public class UpdatePlaylistRequest
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public List<int> SongIds { get; set; }
    }
}
