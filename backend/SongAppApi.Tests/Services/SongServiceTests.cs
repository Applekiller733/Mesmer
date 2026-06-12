using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Moq;
using SongAppApi.Authorization;
using SongAppApi.Entities;
using SongAppApi.Helpers;
using SongAppApi.Helpers.Enumerators;
using SongAppApi.Models.Songs;
using SongAppApi.Services;
using SongAppApi.Tests.Common;
using Xunit;
using EntityFile = SongAppApi.Entities.File;

namespace SongAppApi.Tests.Services
{
    public class SongServiceTests : IDisposable
    {
        private readonly DataContext _context;
        private readonly Mock<IFileService> _fileService;
        private readonly SongService _service;

        public SongServiceTests()
        {
            _context = TestDbContextFactory.Create();
            var mapper = TestMapperFactory.Create(_context);
            _fileService = new Mock<IFileService>();
            _service = new SongService(_context, Mock.Of<IJwtUtils>(), mapper, _fileService.Object);
        }

        public void Dispose() => _context.Dispose();

        [Fact]
        public void Get_ExistingSong_Returns()
        {
            var owner = EntityBuilder.NewAccount();
            var song = EntityBuilder.NewSong(owner);
            _context.Accounts.Add(owner);
            _context.Songs.Add(song);
            _context.SaveChanges();

            var result = _service.Get(song.Id.ToString());

            result.Id.Should().Be(song.Id.ToString());
        }

        [Fact]
        public void Get_Missing_Throws()
        {
            var act = () => _service.Get(Guid.NewGuid().ToString());
            act.Should().Throw<KeyNotFoundException>();
        }

        [Fact]
        public void GetAll_ReturnsAllSongs()
        {
            var owner = EntityBuilder.NewAccount();
            _context.Accounts.Add(owner);
            _context.Songs.AddRange(
                EntityBuilder.NewSong(owner),
                EntityBuilder.NewSong(owner),
                EntityBuilder.NewSong(owner));
            _context.SaveChanges();

            _service.GetAll().Should().HaveCount(3);
        }

        [Fact]
        public void GetAllIds_ReturnsIds()
        {
            var owner = EntityBuilder.NewAccount();
            var s1 = EntityBuilder.NewSong(owner);
            var s2 = EntityBuilder.NewSong(owner);
            _context.Accounts.Add(owner);
            _context.Songs.AddRange(s1, s2);
            _context.SaveChanges();

            var ids = _service.GetAllIds().ToList();

            ids.Should().BeEquivalentTo(new[] { s1.Id.ToString(), s2.Id.ToString() });
        }


        [Fact]
        public void Create_WithoutSoundFile_KeepsExternalUrl()
        {
            var owner = EntityBuilder.NewAccount();
            _context.Accounts.Add(owner);
            _context.SaveChanges();

            var result = _service.Create(new CreateSongRequest
            {
                Name = "S",
                Artist = "A",
                SoundUrl = "https://external.example.com/song.mp3",
            }, owner);

            result.Name.Should().Be("S");
            _fileService.Verify(f => f.CreateFromFormFile(
                It.IsAny<IFormFile>(), It.IsAny<string>(), It.IsAny<FileCategory>()), Times.Never);
        }

        [Fact]
        public void Create_WithSoundFile_CallsFileServiceAndClearsExternalUrl()
        {
            var owner = EntityBuilder.NewAccount();
            _context.Accounts.Add(owner);
            _context.SaveChanges();

            var fileEntity = new EntityFile
            {
                Id = Guid.NewGuid(),
                FileName = "song.mp3",
                Extension = "mp3",
                FilePath = "/tmp/song.mp3"
            };
            _fileService.Setup(f => f.CreateFromFormFile(
                    It.IsAny<IFormFile>(), It.IsAny<string>(), FileCategory.Audio))
                .Returns(fileEntity);

            var mockFormFile = new Mock<IFormFile>();
            mockFormFile.Setup(f => f.Length).Returns(1024);
            mockFormFile.Setup(f => f.FileName).Returns("song.mp3");

            var result = _service.Create(new CreateSongRequest
            {
                Name = "S",
                Artist = "A",
                SoundUrl = "https://should-be-cleared.example.com/x.mp3",
                SoundFile = mockFormFile.Object,
            }, owner);

            var stored = _context.Songs.Single(s => s.Id.ToString() == result.Id);
            stored.SoundId.Should().Be(fileEntity.Id);
            stored.SoundUrl.Should().BeNull();
            _fileService.Verify(f => f.CreateFromFormFile(
                It.IsAny<IFormFile>(), It.IsAny<string>(), FileCategory.Audio), Times.Once);
        }


        [Fact]
        public void Delete_RemovesSong()
        {
            var owner = EntityBuilder.NewAccount();
            var song = EntityBuilder.NewSong(owner);
            _context.Accounts.Add(owner);
            _context.Songs.Add(song);
            _context.SaveChanges();

            _service.Delete(song.Id.ToString());

            _context.Songs.Any(s => s.Id == song.Id).Should().BeFalse();
        }

        [Fact]
        public void Delete_Missing_Throws()
        {
            var act = () => _service.Delete(Guid.NewGuid().ToString());
            act.Should().Throw<KeyNotFoundException>();
        }


        [Fact]
        public void Update_SetsGenre()
        {
            var owner = EntityBuilder.NewAccount();
            var song = EntityBuilder.NewSong(owner);
            _context.Accounts.Add(owner);
            _context.Songs.Add(song);
            _context.SaveChanges();

            var result = _service.Update(new UpdateSongRequest
            {
                Id = song.Id.ToString(),
                Genre = Genre.Rock,
            });

            result.Genre.Should().Be(Genre.Rock);
            _context.Songs.Single(s => s.Id == song.Id).Genre.Should().Be(Genre.Rock);
        }

        [Fact]
        public void Update_ChangesNameAndArtist()
        {
            var owner = EntityBuilder.NewAccount();
            var song = EntityBuilder.NewSong(owner, name: "Old", artist: "OldArtist");
            _context.Accounts.Add(owner);
            _context.Songs.Add(song);
            _context.SaveChanges();

            var result = _service.Update(new UpdateSongRequest
            {
                Id = song.Id.ToString(),
                Name = "New",
                Artist = "NewArtist",
            });

            result.Name.Should().Be("New");
            result.Artist.Should().Be("NewArtist");
        }

        [Fact]
        public void Update_BlankFields_LeaveExistingValuesUntouched()
        {
            var owner = EntityBuilder.NewAccount();
            var song = EntityBuilder.NewSong(owner, name: "Keep", artist: "KeepArtist");
            song.Genre = Genre.Pop;
            _context.Accounts.Add(owner);
            _context.Songs.Add(song);
            _context.SaveChanges();

            var result = _service.Update(new UpdateSongRequest { Id = song.Id.ToString() });

            result.Name.Should().Be("Keep");
            result.Artist.Should().Be("KeepArtist");
            result.Genre.Should().Be(Genre.Pop);
        }

        [Fact]
        public void Update_Missing_Throws()
        {
            var act = () => _service.Update(new UpdateSongRequest { Id = Guid.NewGuid().ToString() });
            act.Should().Throw<KeyNotFoundException>();
        }


        [Fact]
        public void FlipLike_FromNotLiked_AddsLike()
        {
            var owner = EntityBuilder.NewAccount();
            var liker = EntityBuilder.NewAccount();
            var song = EntityBuilder.NewSong(owner);
            _context.Accounts.AddRange(owner, liker);
            _context.Songs.Add(song);
            _context.SaveChanges();

            var result = _service.FlipLike(song.Id.ToString(), liker);

            result.Upvotes.Should().Be(1);
            var reloaded = _context.Songs.Single(s => s.Id == song.Id);
            reloaded.LikedByAccounts.Should().Contain(a => a.Id == liker.Id);
        }

        [Fact]
        public void FlipLike_FromLiked_RemovesLike()
        {
            var owner = EntityBuilder.NewAccount();
            var liker = EntityBuilder.NewAccount();
            var song = EntityBuilder.NewSong(owner);
            song.LikedByAccounts = new List<Account> { liker };
            song.Upvotes = 1;
            _context.Accounts.AddRange(owner, liker);
            _context.Songs.Add(song);
            _context.SaveChanges();

            var result = _service.FlipLike(song.Id.ToString(), liker);

            result.Upvotes.Should().Be(0);
            var reloaded = _context.Songs.Single(s => s.Id == song.Id);
            reloaded.LikedByAccounts.Should().NotContain(a => a.Id == liker.Id);
        }

        [Fact]
        public void GetSoundFile_NoUploadedFile_ReturnsNull()
        {
            var owner = EntityBuilder.NewAccount();
            var song = EntityBuilder.NewSong(owner);
            _context.Accounts.Add(owner);
            _context.Songs.Add(song);
            _context.SaveChanges();

            _service.GetSoundFile(song.Id.ToString()).Should().BeNull();
        }

        [Fact]
        public void GetSoundFile_WithUploadedFile_ReturnsFile()
        {
            var owner = EntityBuilder.NewAccount();
            var file = new EntityFile
            {
                Id = Guid.NewGuid(),
                FileName = "song.mp3",
                Extension = "mp3",
                FilePath = "/tmp/song.mp3",
            };
            var song = EntityBuilder.NewSong(owner);
            song.SoundId = file.Id;
            song.Sound = file;
            _context.Accounts.Add(owner);
            _context.Files.Add(file);
            _context.Songs.Add(song);
            _context.SaveChanges();

            var result = _service.GetSoundFile(song.Id.ToString());

            result.Should().NotBeNull();
            result!.Id.Should().Be(file.Id);
        }
    }
}
