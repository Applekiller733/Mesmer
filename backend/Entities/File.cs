using System.ComponentModel.DataAnnotations;

namespace SongAppApi.Entities
{
    public class File
    {
        [Key]
        public int Id { get; set; }
        public string FileName { get; set; }

        public string Extension { get; set; }
        public string FilePath { get; set; }
        //public IFormFile FormFile { get; set; }
    }
}
