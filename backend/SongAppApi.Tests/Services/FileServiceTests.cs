using System.Net;
using Amazon.S3;
using Amazon.S3.Model;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Moq;
using SongAppApi.Authorization;
using SongAppApi.Helpers;
using SongAppApi.Helpers.Enumerators;
using SongAppApi.Models.Files;
using SongAppApi.Services;
using SongAppApi.Tests.Common;
using Xunit;

namespace SongAppApi.Tests.Services
{
    public class FileServiceTests : IDisposable
    {
        private const string Bucket = "test-media-bucket";

        private readonly DataContext _context;
        private readonly Mock<IAmazonS3> _s3 = new();
        private readonly FileService _service;

        public FileServiceTests()
        {
            _context = TestDbContextFactory.Create();
            var mapper = TestMapperFactory.Create(_context);

            _s3.Setup(s => s.PutObjectAsync(
                    It.IsAny<PutObjectRequest>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new PutObjectResponse());

            _service = new FileService(
                _context,
                Mock.Of<IJwtUtils>(),
                mapper,
                TestSettings.AppSettings(),
                TestSettings.FileUploadSettings(),
                _s3.Object,
                Configuration());
        }

        public void Dispose()
        {
            _context.Dispose();
        }

        private static IConfiguration Configuration() =>
            new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["MEDIA_BUCKET_NAME"] = Bucket
                })
                .Build();

        private static IFormFile MakeFormFile(string fileName, byte[] content)
        {
            var mock = new Mock<IFormFile>();
            mock.Setup(f => f.FileName).Returns(fileName);
            mock.Setup(f => f.Length).Returns(content.Length);
            mock.Setup(f => f.CopyTo(It.IsAny<Stream>()))
                .Callback<Stream>(s => s.Write(content, 0, content.Length));
            mock.Setup(f => f.CopyToAsync(It.IsAny<Stream>(), It.IsAny<CancellationToken>()))
                .Returns<Stream, CancellationToken>((s, ct) =>
                {
                    s.Write(content, 0, content.Length);
                    return Task.CompletedTask;
                });
            return mock.Object;
        }


        [Fact]
        public void Constructor_EmptyAllowedExtensions_Throws()
        {
            var bad = Microsoft.Extensions.Options.Options.Create(new FileUploadSettings
            {
                Image = new CategorySettings { MaxSizeMb = 5, AllowedExtensions = new List<string>() },
                Audio = new CategorySettings { MaxSizeMb = 5, AllowedExtensions = new List<string> { ".mp3" } },
                Video = new CategorySettings { MaxSizeMb = 5, AllowedExtensions = new List<string> { ".mp4" } },
            });

            var act = () => new FileService(
                _context, Mock.Of<IJwtUtils>(),
                TestMapperFactory.Create(_context),
                TestSettings.AppSettings(), bad,
                _s3.Object, Configuration());

            act.Should().Throw<InvalidOperationException>();
        }

        [Fact]
        public void Constructor_MissingBucketName_Throws()
        {
            var emptyConfig = new ConfigurationBuilder().Build();

            var act = () => new FileService(
                _context, Mock.Of<IJwtUtils>(),
                TestMapperFactory.Create(_context),
                TestSettings.AppSettings(), TestSettings.FileUploadSettings(),
                _s3.Object, emptyConfig);

            act.Should().Throw<InvalidOperationException>()
                .WithMessage("*MEDIA_BUCKET_NAME*");
        }


        [Fact]
        public void CreateFromFormFile_ValidAudio_UploadsAndPersists()
        {
            var file = MakeFormFile("song.mp3", new byte[] { 1, 2, 3 });

            var result = _service.CreateFromFormFile(file, "Songs/Audio", FileCategory.Audio);

            result.Should().NotBeNull();
            result.Extension.Should().Be("mp3");
            // FilePath now holds the S3 object key, not a local path.
            result.FilePath.Should().StartWith("Songs/Audio/");
            result.FilePath.Should().EndWith(".mp3");
            _context.Files.Any(f => f.Id == result.Id).Should().BeTrue();

            _s3.Verify(s => s.PutObjectAsync(
                It.Is<PutObjectRequest>(r => r.BucketName == Bucket && r.Key == result.FilePath),
                It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public void CreateFromFormFile_FilenameIsSanitised()
        {
            // the service replaces the original filename with a GUID-based safe
            // key, so the stored key must not carry the attacker-controlled name.
            var file = MakeFormFile("../../../malicious.mp3", new byte[] { 1 });

            var result = _service.CreateFromFormFile(file, "Songs/Audio", FileCategory.Audio);

            result.FilePath.Should().NotContain("malicious");
            result.FilePath.Should().StartWith("Songs/Audio/");
            result.FilePath.Should().EndWith(".mp3");
        }

        [Fact]
        public void CreateFromFormFile_DisallowedExtension_Throws()
        {
            var file = MakeFormFile("evil.exe", new byte[] { 1 });

            var act = () => _service.CreateFromFormFile(file, "Songs/Audio", FileCategory.Audio);

            act.Should().Throw<InvalidOperationException>()
                .WithMessage("*not allowed*");
        }

        [Fact]
        public void CreateFromFormFile_NoExtension_Throws()
        {
            var file = MakeFormFile("nofile", new byte[] { 1 });

            var act = () => _service.CreateFromFormFile(file, "Songs/Audio", FileCategory.Audio);

            act.Should().Throw<InvalidOperationException>();
        }

        [Fact]
        public void CreateFromFormFile_TooLarge_Throws()
        {
            var mock = new Mock<IFormFile>();
            mock.Setup(f => f.FileName).Returns("big.mp3");
            mock.Setup(f => f.Length).Returns(51L * 1024 * 1024);
            // CopyTo never called because the size check throws first
            var act = () => _service.CreateFromFormFile(
                mock.Object, "Songs/Audio", FileCategory.Audio);

            act.Should().Throw<InvalidOperationException>()
                .WithMessage("*too large*");
        }

        [Fact]
        public void CreateFromFormFile_EmptyFile_Throws()
        {
            var mock = new Mock<IFormFile>();
            mock.Setup(f => f.FileName).Returns("x.mp3");
            mock.Setup(f => f.Length).Returns(0);

            var act = () => _service.CreateFromFormFile(
                mock.Object, "Songs/Audio", FileCategory.Audio);

            act.Should().Throw<ArgumentException>();
        }

        [Fact]
        public void CreateFromFormFile_CaseInsensitiveExtension()
        {
            var file = MakeFormFile("song.MP3", new byte[] { 1 });

            var result = _service.CreateFromFormFile(file, "Songs/Audio", FileCategory.Audio);

            result.Should().NotBeNull();
        }


        [Fact]
        public void GetFileById_Existing_Returns()
        {
            var file = MakeFormFile("a.mp3", new byte[] { 1 });
            var saved = _service.CreateFromFormFile(file, "Songs/Audio", FileCategory.Audio);

            var result = _service.GetFileById(saved.Id.ToString());

            result.Id.Should().Be(saved.Id);
        }

        [Fact]
        public void GetFileById_Missing_Throws()
        {
            var act = () => _service.GetFileById(Guid.NewGuid().ToString());
            act.Should().Throw<KeyNotFoundException>();
        }


        [Fact]
        public void GetPresignedDownloadUrl_ReturnsSignedUrl()
        {
            var file = MakeFormFile("a.mp3", new byte[] { 1 });
            var saved = _service.CreateFromFormFile(file, "Songs/Audio", FileCategory.Audio);

            const string signed = "https://s3.example.com/presigned";
            _s3.Setup(s => s.GetPreSignedURL(
                    It.Is<GetPreSignedUrlRequest>(r => r.BucketName == Bucket && r.Key == saved.FilePath)))
                .Returns(signed);

            var url = _service.GetPresignedDownloadUrl(saved);

            url.Should().Be(signed);
        }


        [Fact]
        public void PresignUpload_ValidAudio_ReturnsPutUrlAndKey()
        {
            const string putUrl = "https://s3.example.com/put";
            _s3.Setup(s => s.GetPreSignedURL(
                    It.Is<GetPreSignedUrlRequest>(r =>
                        r.BucketName == Bucket && r.Verb == HttpVerb.PUT)))
                .Returns(putUrl);

            var result = _service.PresignUpload(FileCategory.Audio, "song.mp3");

            result.Url.Should().Be(putUrl);
            result.Key.Should().StartWith("Songs/Audio/");
            result.Key.Should().EndWith(".mp3");
            result.ExpiresAtUtc.Should().BeAfter(DateTime.UtcNow);
        }

        [Fact]
        public void PresignUpload_DisallowedExtension_Throws()
        {
            var act = () => _service.PresignUpload(FileCategory.Audio, "evil.exe");

            act.Should().Throw<InvalidOperationException>()
                .WithMessage("*not allowed*");
        }

        [Fact]
        public void ConfirmUpload_Valid_PersistsRow()
        {
            var metadata = new GetObjectMetadataResponse();
            metadata.Headers.ContentLength = 1024;
            _s3.Setup(s => s.GetObjectMetadataAsync(
                    Bucket, "Songs/Audio/abc.mp3", It.IsAny<CancellationToken>()))
                .ReturnsAsync(metadata);

            var result = _service.ConfirmUpload("Songs/Audio/abc.mp3", "song.mp3", FileCategory.Audio);

            result.FilePath.Should().Be("Songs/Audio/abc.mp3");
            result.Extension.Should().Be("mp3");
            _context.Files.Any(f => f.Id == result.Id).Should().BeTrue();
        }

        [Fact]
        public void ConfirmUpload_TooLarge_DeletesAndThrows()
        {
            var metadata = new GetObjectMetadataResponse();
            metadata.Headers.ContentLength = 51L * 1024 * 1024; // over the 50 MB audio limit
            _s3.Setup(s => s.GetObjectMetadataAsync(
                    It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(metadata);
            _s3.Setup(s => s.DeleteObjectAsync(
                    It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new DeleteObjectResponse());

            var act = () => _service.ConfirmUpload("Songs/Audio/big.mp3", "big.mp3", FileCategory.Audio);

            act.Should().Throw<InvalidOperationException>().WithMessage("*too large*");
            _s3.Verify(s => s.DeleteObjectAsync(
                Bucket, "Songs/Audio/big.mp3", It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public void ConfirmUpload_ObjectMissing_Throws()
        {
            _s3.Setup(s => s.GetObjectMetadataAsync(
                    It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ThrowsAsync(new AmazonS3Exception("not found") { StatusCode = HttpStatusCode.NotFound });

            var act = () => _service.ConfirmUpload("Songs/Audio/missing.mp3", "x.mp3", FileCategory.Audio);

            act.Should().Throw<KeyNotFoundException>();
        }
    }
}
