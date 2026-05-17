using SongAppApi.Helpers.Enumerators;

namespace SongAppApi.Models.Playlist
{
    public class UpdatePlaylistVisibilityRequest
    {
        public PlaylistVisibility Visibility { get; set; }
    }
}