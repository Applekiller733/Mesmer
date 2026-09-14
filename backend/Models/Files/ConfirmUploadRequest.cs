using SongAppApi.Helpers.Enumerators;

namespace SongAppApi.Models.Files
{
    public class ConfirmUploadRequest
    {
        // The object key returned by presign-upload and PUT to by the client.
        public string Key { get; set; } = string.Empty;

        // Original file name to record on the File row.
        public string FileName { get; set; } = string.Empty;

        // Category, so the server can enforce the right max size on confirm.
        public FileCategory Category { get; set; }
    }
}
