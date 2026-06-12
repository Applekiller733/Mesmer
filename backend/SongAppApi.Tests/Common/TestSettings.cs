using Microsoft.Extensions.Options;
using SongAppApi.Helpers;
using SongAppApi.Helpers.Enumerators;

namespace SongAppApi.Tests.Common
{
    public static class TestSettings
    {
        public static IOptions<AppSettings> AppSettings(
            string secret = "test-secret-key-must-be-long-enough-for-hmac-sha256-signing",
            int refreshTokenTTL = 3)
        {
            return Options.Create(new AppSettings
            {
                Secret = secret,
                RefreshTokenTTL = refreshTokenTTL,
                EmailFrom = "test@example.com",
                SmtpHost = "localhost",
                SmtpPort = 25,
                SmtpUser = "test",
                SmtpPass = "test",
            });
        }

        public static IOptions<FileUploadSettings> FileUploadSettings()
        {
            return Options.Create(new FileUploadSettings
            {
                Image = new CategorySettings
                {
                    MaxSizeMb = 5,
                    AllowedExtensions = new List<string> { ".jpg", ".jpeg", ".png", ".gif", ".webp" }
                },
                Audio = new CategorySettings
                {
                    MaxSizeMb = 50,
                    AllowedExtensions = new List<string> { ".mp3", ".wav", ".flac", ".m4a", ".ogg" }
                },
                Video = new CategorySettings
                {
                    MaxSizeMb = 200,
                    AllowedExtensions = new List<string> { ".mp4", ".webm", ".mov" }
                },
            });
        }
    }
}
