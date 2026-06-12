namespace SongAppApi.Helpers
{
    using SongAppApi.Helpers.Enumerators;
    
    public class FileUploadSettings
    {
        public CategorySettings Image { get; set; } = new();
        public CategorySettings Audio { get; set; } = new();
        public CategorySettings Video { get; set; } = new();

        public IReadOnlyDictionary<FileCategory, CategorySettings> AsDictionary() =>
            new Dictionary<FileCategory, CategorySettings>
            {
                [FileCategory.Image] = Image,
                [FileCategory.Audio] = Audio,
                [FileCategory.Video] = Video,
            };
    }

    public class CategorySettings
    {
        
        public int MaxSizeMb { get; set; } = 10;
        public List<string> AllowedExtensions { get; set; } = new();

        public long MaxSizeBytes => MaxSizeMb * 1024L * 1024L;
    }
}