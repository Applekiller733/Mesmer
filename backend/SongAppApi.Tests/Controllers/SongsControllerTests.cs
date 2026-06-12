using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Moq;
using SongAppApi.Controllers;
using SongAppApi.Entities;
using SongAppApi.Helpers.Enumerators;
using SongAppApi.Models.Accounts;
using SongAppApi.Models.Songs;
using SongAppApi.Services;
using SongAppApi.Tests.Common;
using Xunit;
using EntityFile = SongAppApi.Entities.File;

namespace SongAppApi.Tests.Controllers
{
    public class SongsControllerTests
    {
        private readonly Mock<ISongService> _songService = new();
        private readonly Mock<IAccountService> _accountService = new();
        private readonly SongsController _controller;

        public SongsControllerTests()
        {
            _controller = new SongsController(_songService.Object, _accountService.Object);
        }


        [Fact]
        public void GetAll_ReturnsOk()
        {
            ControllerContextHelper.Attach(_controller, account: null);
            var songs = new List<SongResponse> { new() { Id = "s1" } };
            _songService.Setup(s => s.GetAll()).Returns(songs);

            var result = _controller.GetAll();

            result.Result.Should().BeOfType<OkObjectResult>()
                .Which.Value.Should().Be(songs);
        }

        [Fact]
        public void GetAll_ServiceThrows_Returns500()
        {
            ControllerContextHelper.Attach(_controller, account: null);
            _songService.Setup(s => s.GetAll()).Throws(new Exception("boom"));

            var result = _controller.GetAll();

            var status = result.Result.Should().BeOfType<ObjectResult>().Subject;
            status.StatusCode.Should().Be(500);
        }


        [Fact]
        public void GetAllIds_ReturnsOk()
        {
            ControllerContextHelper.Attach(_controller, account: null);
            _songService.Setup(s => s.GetAllIds()).Returns(new[] { "a", "b" });

            var result = _controller.GetAllIds();

            result.Result.Should().BeOfType<OkObjectResult>();
        }

        [Fact]
        public void Get_HappyPath_ReturnsOk()
        {
            ControllerContextHelper.Attach(_controller, account: null);
            _songService.Setup(s => s.Get("s1")).Returns(new SongResponse { Id = "s1" });

            var result = _controller.Get("s1");

            result.Result.Should().BeOfType<OkObjectResult>();
        }

        [Fact]
        public void Get_Missing_Returns404()
        {
            ControllerContextHelper.Attach(_controller, account: null);
            _songService.Setup(s => s.Get(It.IsAny<string>()))
                .Throws(new KeyNotFoundException("nope"));

            var result = _controller.Get("missing");

            result.Result.Should().BeOfType<NotFoundObjectResult>();
        }


        [Fact]
        public void Create_NoAccount_ReturnsUnauthorized()
        {
            ControllerContextHelper.Attach(_controller, account: null);

            var result = _controller.Create(new CreateSongRequest { Name = "n", Artist = "a" });

            result.Result.Should().BeOfType<UnauthorizedResult>();
        }

        [Fact]
        public void Create_HappyPath_ReturnsOk()
        {
            var user = EntityBuilder.NewAccount();
            ControllerContextHelper.Attach(_controller, user);
            var resp = new SongResponse { Id = "s1", Name = "n" };
            _songService.Setup(s => s.Create(It.IsAny<CreateSongRequest>(), user))
                .Returns(resp);

            var result = _controller.Create(new CreateSongRequest { Name = "n", Artist = "a" });

            result.Result.Should().BeOfType<OkObjectResult>()
                .Which.Value.Should().Be(resp);
        }

        [Fact]
        public void Create_InvalidOperation_ReturnsBadRequest()
        {
            var user = EntityBuilder.NewAccount();
            ControllerContextHelper.Attach(_controller, user);
            _songService.Setup(s => s.Create(It.IsAny<CreateSongRequest>(), It.IsAny<Account>()))
                .Throws(new InvalidOperationException("File too large."));

            var result = _controller.Create(new CreateSongRequest { Name = "n", Artist = "a" });

            result.Result.Should().BeOfType<BadRequestObjectResult>();
        }

        [Fact]
        public void Create_GenericException_Returns500()
        {
            var user = EntityBuilder.NewAccount();
            ControllerContextHelper.Attach(_controller, user);
            _songService.Setup(s => s.Create(It.IsAny<CreateSongRequest>(), It.IsAny<Account>()))
                .Throws(new Exception("boom"));

            var result = _controller.Create(new CreateSongRequest { Name = "n", Artist = "a" });

            var status = result.Result.Should().BeOfType<ObjectResult>().Subject;
            status.StatusCode.Should().Be(500);
        }


        [Fact]
        public void FlipLike_NoAccount_ReturnsUnauthorized()
        {
            ControllerContextHelper.Attach(_controller, account: null);
            var result = _controller.FlipLike(new FlipLikeRequest { Id = "s1" });
            result.Should().BeOfType<UnauthorizedResult>();
        }

        [Fact]
        public void FlipLike_HappyPath_ReturnsOk()
        {
            var user = EntityBuilder.NewAccount();
            ControllerContextHelper.Attach(_controller, user);
            _songService.Setup(s => s.FlipLike("s1", user))
                .Returns(new UpvotesResponse { Id = "s1", Upvotes = 1 });

            var result = _controller.FlipLike(new FlipLikeRequest { Id = "s1" });

            result.Should().BeOfType<OkObjectResult>();
        }


        [Fact]
        public void Delete_NoAccount_ReturnsUnauthorized()
        {
            ControllerContextHelper.Attach(_controller, account: null);
            var result = _controller.Delete(new DeleteSongRequest { Id = "s1" });
            result.Should().BeOfType<UnauthorizedResult>();
        }

        [Fact]
        public void Delete_NonCreatorNonAdmin_ReturnsUnauthorized()
        {
            var user = EntityBuilder.NewAccount();
            ControllerContextHelper.Attach(_controller, user);
            _songService.Setup(s => s.Get("s1")).Returns(new SongResponse
            {
                Id = "s1",
                CreatedBy = new AccountResponse { Id = Guid.NewGuid().ToString() },
            });

            var result = _controller.Delete(new DeleteSongRequest { Id = "s1" });

            result.Should().BeOfType<UnauthorizedObjectResult>();
            _songService.Verify(s => s.Delete(It.IsAny<string>()), Times.Never);
        }

        [Fact]
        public void Delete_Creator_DeletesAndReturnsOk()
        {
            var user = EntityBuilder.NewAccount();
            ControllerContextHelper.Attach(_controller, user);
            _songService.Setup(s => s.Get("s1")).Returns(new SongResponse
            {
                Id = "s1",
                CreatedBy = new AccountResponse { Id = user.Id.ToString() },
            });

            var result = _controller.Delete(new DeleteSongRequest { Id = "s1" });

            result.Should().BeOfType<OkObjectResult>();
            _songService.Verify(s => s.Delete("s1"), Times.Once);
        }

        [Fact]
        public void Delete_AdminCanDeleteAnything()
        {
            var admin = EntityBuilder.NewAccount(role: Role.Admin);
            ControllerContextHelper.Attach(_controller, admin);
            _songService.Setup(s => s.Get("s1")).Returns(new SongResponse
            {
                Id = "s1",
                CreatedBy = new AccountResponse { Id = Guid.NewGuid().ToString() },
            });

            var result = _controller.Delete(new DeleteSongRequest { Id = "s1" });

            result.Should().BeOfType<OkObjectResult>();
            _songService.Verify(s => s.Delete("s1"), Times.Once);
        }


        [Fact]
        public void Update_NoAccount_ReturnsUnauthorized()
        {
            ControllerContextHelper.Attach(_controller, account: null);

            var result = _controller.Update(new UpdateSongRequest { Id = "s1" });

            result.Result.Should().BeOfType<UnauthorizedResult>();
        }

        [Fact]
        public void Update_HappyPath_ReturnsOk()
        {
            var admin = EntityBuilder.NewAccount(role: Role.Admin);
            ControllerContextHelper.Attach(_controller, admin);
            var resp = new SongResponse { Id = "s1", Genre = Genre.Rock };
            _songService.Setup(s => s.Update(It.IsAny<UpdateSongRequest>())).Returns(resp);

            var result = _controller.Update(new UpdateSongRequest { Id = "s1", Genre = Genre.Rock });

            result.Result.Should().BeOfType<OkObjectResult>()
                .Which.Value.Should().Be(resp);
        }

        [Fact]
        public void Update_Missing_Returns404()
        {
            var admin = EntityBuilder.NewAccount(role: Role.Admin);
            ControllerContextHelper.Attach(_controller, admin);
            _songService.Setup(s => s.Update(It.IsAny<UpdateSongRequest>()))
                .Throws(new KeyNotFoundException("nope"));

            var result = _controller.Update(new UpdateSongRequest { Id = "missing" });

            result.Result.Should().BeOfType<NotFoundObjectResult>();
        }

        [Fact]
        public void Update_GenericException_ReturnsBadRequest()
        {
            var admin = EntityBuilder.NewAccount(role: Role.Admin);
            ControllerContextHelper.Attach(_controller, admin);
            _songService.Setup(s => s.Update(It.IsAny<UpdateSongRequest>()))
                .Throws(new Exception("boom"));

            var result = _controller.Update(new UpdateSongRequest { Id = "s1" });

            result.Result.Should().BeOfType<BadRequestObjectResult>();
        }


        [Fact]
        public void GetAudio_NoFile_Returns404()
        {
            ControllerContextHelper.Attach(_controller, account: null);
            _songService.Setup(s => s.GetSoundFile("s1")).Returns((EntityFile?)null);

            var result = _controller.GetAudio("s1");

            result.Should().BeOfType<NotFoundResult>();
        }
    }
}
