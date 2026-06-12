using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Moq;
using SongAppApi.Authorization;
using SongAppApi.Helpers;
using SongAppApi.Helpers.Enumerators;
using SongAppApi.Services;
using SongAppApi.Tests.Common;
using Xunit;

namespace SongAppApi.Tests.Services
{
    public class FileServiceTests : IDisposable
    {
        private readonly string _workingDir;
        private readonly string _originalCwd;
        private readonly DataContext _context;
        private readonly FileService _service;

        public FileServiceTests()
        {
            _originalCwd = Directory.GetCurrentDirectory();
            _workingDir = Path.Combine(Path.GetTempPath(), "fs-tests-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(_workingDir);
            Directory.SetCurrentDirectory(_workingDir);

            _context = TestDbContextFactory.Create();
            var mapper = TestMapperFactory.Create(_context);
            _service = new FileService(
                _context,
                Mock.Of<IJwtUtils>(),
                mapper,
                TestSettings.AppSettings(),
                TestSettings.FileUploadSettings());
        }

        public void Dispose()
        {
            _context.Dispose();
            Directory.SetCurrentDirectory(_originalCwd);
            try { Directory.Delete(_workingDir, recursive: true); } catch { /* best effort */ }
        }

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
                TestSettings.AppSettings(), bad);

            act.Should().Throw<InvalidOperationException>();
        }


        [Fact]
        public void CreateFromFormFile_ValidAudio_Persists()
        {
            var file = MakeFormFile("song.mp3", new byte[] { 1, 2, 3 });

            var result = _service.CreateFromFormFile(file, "Songs/Audio", FileCategory.Audio);

            result.Should().NotBeNull();
            result.Extension.Should().Be("mp3");
            File.Exists(result.FilePath).Should().BeTrue();
            _context.Files.Any(f => f.Id == result.Id).Should().BeTrue();
        }

        [Fact]
        public void CreateFromFormFile_FilenameIsSanitised()
        {
            // the service replaces the original filename with a GUID-based safe
            // name, we assert the file lands at a different path than the input.
            var file = MakeFormFile("../../../malicious.mp3", new byte[] { 1 });

            var result = _service.CreateFromFormFile(file, "Songs/Audio", FileCategory.Audio);

            Path.GetFileName(result.FilePath).Should().NotBe("malicious.mp3");
            Path.GetFileName(result.FilePath).Should().EndWith(".mp3");
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
            var bigPayload = new byte[1]; // mock the size separately so we don't allocate 50MB
            var mock = new Mock<IFormFile>();
            mock.Setup(f => f.FileName).Returns("big.mp3");
            mock.Setup(f => f.Length).Returns(51L * 1024 * 1024);
            // CopyTo never called because size check throws first
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
        public void VerifyExistingDirectory_True()
        {
            _service.VerifyExistingDirectory(_workingDir).Should().BeTrue();
        }

        [Fact]
        public void VerifyExistingDirectory_False()
        {
            _service.VerifyExistingDirectory(
                Path.Combine(_workingDir, "does-not-exist")).Should().BeFalse();
        }
    }
}
