namespace SongAppApi.Models.Files
{
    public class FileModel
    {
        public string FileName { get; set; }

        public string Extension { get; set; }
        public IFormFile FormFile { get; set; }
    }
}
