namespace SongAppApi.Helpers
{
    using SongAppApi.Helpers.Enumerators;
    /// <summary>
    /// Strongly-typed binding target for the "FileUpload" section in appsettings.json.
    /// One CategorySettings entry per FileCategory enum value.
    /// </summary>
    public class FileUploadSettings
    {
        public CategorySettings Image { get; set; } = new();
        public CategorySettings Audio { get; set; } = new();
        public CategorySettings Video { get; set; } = new();

        /// <summary>
        /// Returns the per-category settings as a dictionary keyed by FileCategory,
        /// so FileService can look them up dynamically without a switch statement.
        /// </summary>
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
        /// <summary>
        /// Maximum file size in megabytes. The service converts to bytes once at use time.
        /// Stored as MB so the config file stays human-readable.
        /// </summary>
        public int MaxSizeMb { get; set; } = 10;

        /// <summary>
        /// Allowed file extensions including the leading dot (e.g. ".mp3").
        /// Comparison is case-insensitive at the service layer.
        /// </summary>
        public List<string> AllowedExtensions { get; set; } = new();

        public long MaxSizeBytes => MaxSizeMb * 1024L * 1024L;
    }
}