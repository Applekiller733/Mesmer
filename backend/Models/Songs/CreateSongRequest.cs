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

        // External URL fallback. If SoundFile is supplied, this is ignored
        // on the server and the response's SoundUrl will be the streaming endpoint.
        public string? SoundUrl { get; set; }

        // Optional uploaded audio file. When present, FileService persists it
        // to disk and links it to the Song via SoundId.
        public IFormFile? SoundFile { get; set; }
    }
}