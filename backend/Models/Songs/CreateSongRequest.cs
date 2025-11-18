using System.ComponentModel.DataAnnotations;

namespace SongAppApi.Models.Songs
{
    public class CreateSongRequest
    {
        [Required]
        public string Name { get; set; }
        [Required]
        public string Artist { get; set; }
        public string? ImageUrl { get; set; }
        public string? VideoUrl { get; set; }
        public string? SoundUrl { get; set; }
    }
}
