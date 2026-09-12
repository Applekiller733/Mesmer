using AutoMapper;
using System.Net;
using Microsoft.AspNetCore.StaticFiles;
using Microsoft.Extensions.Options;
using Amazon.S3;
using Amazon.S3.Model;
using SongAppApi.Authorization;
using SongAppApi.Helpers;
using SongAppApi.Models.Files;
using File = SongAppApi.Entities.File;
using SongAppApi.Helpers.Enumerators;

namespace SongAppApi.Services
{
    public interface IFileService
    {
        string Create(FileModel file);
        string Create(FileModel file, string subfolderpath);
        File CreateFromFormFile(IFormFile formFile, string subfolderpath, FileCategory category);
        File GetFileById(string id);
        // Short-lived presigned GET URL so clients fetch bytes straight from S3
        // instead of streaming them through the API/Lambda.
        string GetPresignedDownloadUrl(File file, TimeSpan? expiresIn = null);
        // Issue a short-lived presigned PUT URL so the client uploads bytes
        // straight to S3 (bypassing the API Gateway payload limit). Validates the
        // extension and reserves the object key up front.
        PresignedUpload PresignUpload(FileCategory category, string originalFileName,
            string? keyPrefix = null, TimeSpan? expiresIn = null);
        // Verify a presigned upload actually landed (and is within size limits),
        // then persist the File row. Returns the created entity.
        File ConfirmUpload(string objectKey, string originalFileName, FileCategory category);
    }

    public class FileService : IFileService
    {
        private readonly DataContext _context;
        private readonly IJwtUtils _jwtUtils;
        private readonly IMapper _mapper;
        private readonly AppSettings _settings;
        private readonly FileUploadSettings _uploadSettings;
        private readonly IAmazonS3 _s3;
        private readonly string _bucket;

        private static readonly FileExtensionContentTypeProvider ContentTypeProvider = new();
        private static readonly TimeSpan DefaultUrlLifetime = TimeSpan.FromMinutes(15);
        private static readonly TimeSpan DefaultUploadUrlLifetime = TimeSpan.FromMinutes(15);

        // cached lookup for case-insensitive extension matching
        // built once at construction; appsettings.json reload would require restart
        private readonly IReadOnlyDictionary<FileCategory, HashSet<string>> _allowedExtensions;
        private readonly IReadOnlyDictionary<FileCategory, long> _maxSizeBytes;

        public FileService(DataContext context, IJwtUtils jwtUtils,
            IMapper mapper, IOptions<AppSettings> settings,
            IOptions<FileUploadSettings> uploadSettings,
            IAmazonS3 s3, IConfiguration configuration)
        {
            _context = context;
            _jwtUtils = jwtUtils;
            _mapper = mapper;
            _settings = settings.Value;
            _uploadSettings = uploadSettings.Value;
            _s3 = s3;
            _bucket = configuration["MEDIA_BUCKET_NAME"]
                ?? throw new InvalidOperationException(
                    "MEDIA_BUCKET_NAME is not configured.");

            // build lookup tables from config
            var asDict = _uploadSettings.AsDictionary();

            _allowedExtensions = asDict.ToDictionary(
                kvp => kvp.Key,
                kvp => new HashSet<string>(
                    kvp.Value.AllowedExtensions ?? new List<string>(),
                    StringComparer.OrdinalIgnoreCase));

            _maxSizeBytes = asDict.ToDictionary(
                kvp => kvp.Key,
                kvp => kvp.Value.MaxSizeBytes);

            foreach (var (category, exts) in _allowedExtensions)
            {
                if (exts.Count == 0)
                    throw new InvalidOperationException(
                        $"FileUpload:{category}:AllowedExtensions is empty in configuration.");
            }
        }

        public string Create(FileModel file)
        {
            return Create(file, subfolderpath: "");
        }

        public string Create(FileModel file, string subfolderpath)
        {
            if (file == null)
                throw new ArgumentNullException(nameof(file));

            var key = BuildKey(subfolderpath, file.FileName);
            UploadToS3(file.FormFile, key, file.FileName);

            File fileEF = new File
            {
                FileName = file.FileName,
                Extension = file.Extension,
                // FilePath now holds the S3 object key, not a local filesystem path.
                FilePath = key
            };

            _context.Files.Add(fileEF);
            _context.SaveChanges();

            return fileEF.Id.ToString();
        }

        public File CreateFromFormFile(IFormFile formFile, string subfolderpath, FileCategory category)
        {
            if (formFile == null || formFile.Length == 0)
                throw new ArgumentException("File is empty.", nameof(formFile));

            // check size
            var maxSize = _maxSizeBytes[category];
            if (formFile.Length > maxSize)
                throw new InvalidOperationException(
                    $"File too large. Maximum {maxSize / (1024 * 1024)} MB for {category}.");

            // chk extension
            var originalName = formFile.FileName;
            var extension = Path.GetExtension(originalName);
            if (string.IsNullOrEmpty(extension) || !_allowedExtensions[category].Contains(extension))
                throw new InvalidOperationException(
                    $"Extension '{extension}' is not allowed for {category}.");

            // sanitize file name into a collision-free object key
            var safeFileName = $"{Guid.NewGuid():N}{extension}";
            var key = BuildKey(subfolderpath, safeFileName);

            UploadToS3(formFile, key, originalName);

            var entity = new File
            {
                FileName = originalName,
                Extension = extension.TrimStart('.'),
                // FilePath now holds the S3 object key, not a local filesystem path.
                FilePath = key
            };

            _context.Files.Add(entity);
            _context.SaveChanges();

            return entity;
        }

        public File GetFileById(string id)
        {
            var file = _context.Files.Find(Guid.Parse(id));
            if (file == null)
            {
                throw new KeyNotFoundException("Could not find file");
            }
            return file;
        }

        public string GetPresignedDownloadUrl(File file, TimeSpan? expiresIn = null)
        {
            if (file == null)
                throw new ArgumentNullException(nameof(file));

            var request = new GetPreSignedUrlRequest
            {
                BucketName = _bucket,
                Key = file.FilePath,
                Verb = HttpVerb.GET,
                Expires = DateTime.UtcNow.Add(expiresIn ?? DefaultUrlLifetime)
            };

            // Local signing operation — no network call, so this stays synchronous.
            return _s3.GetPreSignedURL(request);
        }

        public PresignedUpload PresignUpload(FileCategory category, string originalFileName,
            string? keyPrefix = null, TimeSpan? expiresIn = null)
        {
            if (string.IsNullOrWhiteSpace(originalFileName))
                throw new ArgumentException("File name is required.", nameof(originalFileName));

            var extension = Path.GetExtension(originalFileName);
            if (string.IsNullOrEmpty(extension) || !_allowedExtensions[category].Contains(extension))
                throw new InvalidOperationException(
                    $"Extension '{extension}' is not allowed for {category}.");

            // Reserve a collision-free key now; the client uploads to exactly this key.
            var safeFileName = $"{Guid.NewGuid():N}{extension}";
            var key = BuildKey(keyPrefix ?? DefaultPrefixFor(category), safeFileName);
            var expiresAt = DateTime.UtcNow.Add(expiresIn ?? DefaultUploadUrlLifetime);

            var request = new GetPreSignedUrlRequest
            {
                BucketName = _bucket,
                Key = key,
                Verb = HttpVerb.PUT,
                Expires = expiresAt
            };

            return new PresignedUpload
            {
                Key = key,
                Url = _s3.GetPreSignedURL(request),
                ExpiresAtUtc = expiresAt
            };
        }

        public File ConfirmUpload(string objectKey, string originalFileName, FileCategory category)
        {
            if (string.IsNullOrWhiteSpace(objectKey))
                throw new ArgumentException("Object key is required.", nameof(objectKey));

            // Verify the object actually exists (client really did upload) and read
            // its size, so a presigned URL can't be used to smuggle in an oversized
            // file past the category limit.
            long contentLength;
            try
            {
                var metadata = _s3.GetObjectMetadataAsync(_bucket, objectKey)
                    .GetAwaiter().GetResult();
                contentLength = metadata.Headers.ContentLength;
            }
            catch (AmazonS3Exception ex) when (ex.StatusCode == HttpStatusCode.NotFound)
            {
                throw new KeyNotFoundException("No uploaded object found for the given key.");
            }

            var maxSize = _maxSizeBytes[category];
            if (contentLength > maxSize)
            {
                // Reject and clean up so the oversized object doesn't linger.
                _s3.DeleteObjectAsync(_bucket, objectKey).GetAwaiter().GetResult();
                throw new InvalidOperationException(
                    $"File too large. Maximum {maxSize / (1024 * 1024)} MB for {category}.");
            }

            var extension = Path.GetExtension(originalFileName);
            var entity = new File
            {
                FileName = originalFileName,
                Extension = (extension ?? string.Empty).TrimStart('.'),
                FilePath = objectKey
            };

            _context.Files.Add(entity);
            _context.SaveChanges();

            return entity;
        }

        // Default object-key prefix per category when the caller doesn't supply one.
        private static string DefaultPrefixFor(FileCategory category) => category switch
        {
            FileCategory.Audio => "Songs/Audio",
            FileCategory.Video => "Songs/Video",
            FileCategory.Image => "Images",
            _ => "Misc"
        };

        // Copy the uploaded bytes to S3 under `key`. ASP.NET Core has no
        // synchronization context, so blocking on the async SDK call here does not
        // deadlock; keeping the public API synchronous avoids rippling async
        // through every upload caller. (Presigned PUT uploads would remove this
        // server-side copy entirely — a later refinement.)
        private void UploadToS3(IFormFile formFile, string key, string fileNameForContentType)
        {
            if (!ContentTypeProvider.TryGetContentType(fileNameForContentType, out var contentType))
                contentType = "application/octet-stream";

            using var buffer = new MemoryStream();
            formFile.CopyTo(buffer);
            buffer.Position = 0;

            var request = new PutObjectRequest
            {
                BucketName = _bucket,
                Key = key,
                InputStream = buffer,
                ContentType = contentType
            };

            _s3.PutObjectAsync(request).GetAwaiter().GetResult();
        }

        // Compose an S3 object key from an optional prefix and a file name,
        // normalising separators (S3 keys use '/').
        private static string BuildKey(string subfolderpath, string fileName)
        {
            var key = string.IsNullOrWhiteSpace(subfolderpath)
                ? fileName
                : $"{subfolderpath}/{fileName}";

            return key.Replace('\\', '/').TrimStart('/');
        }
    }
}
