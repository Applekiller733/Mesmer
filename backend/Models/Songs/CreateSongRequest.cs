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

        // Ids of files already uploaded straight to S3 (presign + confirm flow).
        // When set, these take precedence over the *Url fields / SoundFile.
        public Guid? ImageFileId { get; set; }
        public Guid? VideoFileId { get; set; }
        public Guid? SoundFileId { get; set; }

        // Legacy path: small audio uploaded through the API as multipart.
        public IFormFile? SoundFile { get; set; }
    }
}