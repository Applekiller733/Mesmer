using System.ComponentModel.DataAnnotations;
using MassTransit;

namespace SongAppApi.Entities
{
    public class File
    {
        [Key]
        public Guid Id { get; set; } = NewId.NextSequentialGuid();
        public string FileName { get; set; }

        public string Extension { get; set; }
        // S3 object key of the stored file (e.g. "Songs/Audio/{guid}.mp3").
        // Historically this held a local filesystem path; the column name is
        // kept for backwards compatibility, but the meaning is now an S3 key.
        public string FilePath { get; set; }
        //public IFormFile FormFile { get; set; }
    }
}
