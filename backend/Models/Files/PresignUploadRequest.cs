using SongAppApi.Helpers.Enumerators;

namespace SongAppApi.Models.Files
{
    public class PresignUploadRequest
    {
        // Image / Audio / Video — decides which size limit and allowed
        // extensions apply, and the default key prefix.
        public FileCategory Category { get; set; }

        // Original file name (used for extension validation only).
        public string FileName { get; set; } = string.Empty;

        // Optional override for the object-key prefix (e.g. a per-user folder).
        public string? KeyPrefix { get; set; }
    }
}
