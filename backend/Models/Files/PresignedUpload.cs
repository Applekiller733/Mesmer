namespace SongAppApi.Models.Files
{
    // Returned to the client so it can upload bytes straight to S3 with an
    // HTTP PUT to Url, then call confirm-upload with Key.
    public class PresignedUpload
    {
        public string Key { get; set; } = string.Empty;
        public string Url { get; set; } = string.Empty;
        public DateTime ExpiresAtUtc { get; set; }
    }
}
