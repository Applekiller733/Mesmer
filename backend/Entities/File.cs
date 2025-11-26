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
        public string FilePath { get; set; }
        //public IFormFile FormFile { get; set; }
    }
}
