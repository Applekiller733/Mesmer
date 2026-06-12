using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Moq;
using SongAppApi.Controllers;
using SongAppApi.Helpers;
using SongAppApi.Helpers.Enumerators;
using SongAppApi.Models.Accounts;
using SongAppApi.Models.Playlist;
using SongAppApi.Services;
using SongAppApi.Tests.Common;
using Xunit;

namespace SongAppApi.Tests.Controllers
{
    public class PlaylistsControllerTests
    {
        private readonly Mock<IPlaylistService> _service = new();
        private readonly PlaylistsController _controller;

        public PlaylistsControllerTests()
        {
            _controller = new PlaylistsController(_service.Object);
        }


        [Fact]
        public void Get_NoAccount_ReturnsUnauthorized()
        {
            ControllerContextHelper.Attach(_controller, account: null);
            var result = _controller.Get("some-id");
            result.Result.Should().BeOfType<UnauthorizedResult>();
        }

        [Fact]
        public void Get_HappyPath_ReturnsOk()
        {
            var user = EntityBuilder.NewAccount();
            ControllerContextHelper.Attach(_controller, user);
            var response = new PlaylistResponse { Id = "p1", Name = "n" };
            _service.Setup(s => s.Get("p1", user.Id.ToString())).Returns(response);

            var result = _controller.Get("p1");

            result.Result.Should().BeOfType<OkObjectResult>()
                .Which.Value.Should().Be(response);
        }

        [Fact]
        public void Get_KeyNotFound_Returns404()
        {
            var user = EntityBuilder.NewAccount();
            ControllerContextHelper.Attach(_controller, user);
            _service.Setup(s => s.Get(It.IsAny<string>(), It.IsAny<string>()))
                .Throws(new KeyNotFoundException("nope"));

            var result = _controller.Get("p1");

            result.Result.Should().BeOfType<NotFoundObjectResult>();
        }

        [Fact]
        public void Get_OtherException_ReturnsBadRequest()
        {
            var user = EntityBuilder.NewAccount();
            ControllerContextHelper.Attach(_controller, user);
            _service.Setup(s => s.Get(It.IsAny<string>(), It.IsAny<string>()))
                .Throws(new InvalidOperationException("oops"));

            var result = _controller.Get("p1");

            result.Result.Should().BeOfType<BadRequestObjectResult>();
        }


        [Fact]
        public void GetAll_ReturnsOk()
        {
            var admin = EntityBuilder.NewAccount(role: Role.Admin);
            ControllerContextHelper.Attach(_controller, admin);
            var list = new List<PlaylistResponse> { new() { Id = "p1" } };
            _service.Setup(s => s.GetAll()).Returns(list);

            var result = _controller.GetAll();

            result.Result.Should().BeOfType<OkObjectResult>();
        }


        [Fact]
        public void GetAllCreatedByAccount_PassesCurrentUserId()
        {
            var user = EntityBuilder.NewAccount();
            ControllerContextHelper.Attach(_controller, user);
            var target = Guid.NewGuid().ToString();
            _service.Setup(s => s.GetCreatedByAccount(target, user.Id.ToString()))
                .Returns(new List<PlaylistResponse>());

            var result = _controller.GetAllCreatedByAccount(target);

            result.Result.Should().BeOfType<OkObjectResult>();
            _service.Verify(s => s.GetCreatedByAccount(target, user.Id.ToString()), Times.Once);
        }

        [Fact]
        public void GetAllSavedByAccount_PassesIsAdmin()
        {
            var admin = EntityBuilder.NewAccount(role: Role.Admin);
            ControllerContextHelper.Attach(_controller, admin);
            var target = Guid.NewGuid().ToString();
            _service.Setup(s => s.GetSavedByAccount(target, admin.Id.ToString(), true))
                .Returns(new List<PlaylistResponse>());

            var result = _controller.GetAllSavedByAccount(target);

            result.Result.Should().BeOfType<OkObjectResult>();
            _service.Verify(s => s.GetSavedByAccount(target, admin.Id.ToString(), true), Times.Once);
        }


        [Fact]
        public void CreatePlaylist_HappyPath_ReturnsOk()
        {
            var user = EntityBuilder.NewAccount();
            ControllerContextHelper.Attach(_controller, user);
            var req = new CreatePlaylistRequest { Name = "x", SongIds = new() };
            var resp = new PlaylistResponse { Id = "p1", Name = "x" };
            _service.Setup(s => s.Create(req, user)).Returns(resp);

            var result = _controller.CreatePlaylist(req);

            result.Result.Should().BeOfType<OkObjectResult>()
                .Which.Value.Should().Be(resp);
        }

        [Fact]
        public void CreatePlaylist_NoAccount_ReturnsUnauthorized()
        {
            ControllerContextHelper.Attach(_controller, account: null);
            var result = _controller.CreatePlaylist(new CreatePlaylistRequest());
            result.Result.Should().BeOfType<UnauthorizedResult>();
        }


        [Fact]
        public void UpdatePlaylist_PassesIsAdmin()
        {
            var admin = EntityBuilder.NewAccount(role: Role.Admin);
            ControllerContextHelper.Attach(_controller, admin);
            var req = new UpdatePlaylistRequest { Id = "p1", Name = "n", SongIds = new() };
            _service.Setup(s => s.Update("p1", req, admin.Id.ToString(), true))
                .Returns(new PlaylistResponse());

            _controller.UpdatePlaylist(req);

            _service.Verify(s => s.Update("p1", req, admin.Id.ToString(), true), Times.Once);
        }


        [Fact]
        public void Delete_HappyPath_ReturnsOk()
        {
            var user = EntityBuilder.NewAccount();
            ControllerContextHelper.Attach(_controller, user);

            var result = _controller.Delete(new DeletePlaylistRequest { Id = "p1" });

            result.Should().BeOfType<OkObjectResult>();
            _service.Verify(s => s.Delete("p1", user.Id.ToString(), false), Times.Once);
        }

        [Fact]
        public void Delete_KeyNotFound_Returns404()
        {
            var user = EntityBuilder.NewAccount();
            ControllerContextHelper.Attach(_controller, user);
            _service.Setup(s => s.Delete(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<bool>()))
                .Throws(new KeyNotFoundException("nope"));

            var result = _controller.Delete(new DeletePlaylistRequest { Id = "p1" });

            result.Should().BeOfType<NotFoundObjectResult>();
        }

        [Fact]
        public void Delete_NonOwner_ReturnsBadRequest()
        {
            var user = EntityBuilder.NewAccount();
            ControllerContextHelper.Attach(_controller, user);
            _service.Setup(s => s.Delete(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<bool>()))
                .Throws(new AppException("forbidden"));

            var result = _controller.Delete(new DeletePlaylistRequest { Id = "p1" });

            result.Should().BeOfType<BadRequestObjectResult>();
        }


        [Fact]
        public void Save_HappyPath_ReturnsOk()
        {
            var user = EntityBuilder.NewAccount();
            ControllerContextHelper.Attach(_controller, user);
            _service.Setup(s => s.Save("p1", user.Id.ToString()))
                .Returns(new PlaylistResponse { Id = "p1" });

            var result = _controller.Save("p1");

            result.Result.Should().BeOfType<OkObjectResult>();
        }

        [Fact]
        public void Save_KeyNotFound_Returns404()
        {
            var user = EntityBuilder.NewAccount();
            ControllerContextHelper.Attach(_controller, user);
            _service.Setup(s => s.Save(It.IsAny<string>(), It.IsAny<string>()))
                .Throws(new KeyNotFoundException("nope"));

            var result = _controller.Save("p1");

            result.Result.Should().BeOfType<NotFoundObjectResult>();
        }


        [Fact]
        public void Unsave_HappyPath_ReturnsOk()
        {
            var user = EntityBuilder.NewAccount();
            ControllerContextHelper.Attach(_controller, user);

            var result = _controller.Unsave("p1");

            result.Should().BeOfType<OkObjectResult>();
            _service.Verify(s => s.Unsave("p1", user.Id.ToString()), Times.Once);
        }

        [Fact]
        public void Unsave_OwnerAttempt_ReturnsBadRequest()
        {
            var user = EntityBuilder.NewAccount();
            ControllerContextHelper.Attach(_controller, user);
            _service.Setup(s => s.Unsave(It.IsAny<string>(), It.IsAny<string>()))
                .Throws(new AppException("can't unsave"));

            var result = _controller.Unsave("p1");

            result.Should().BeOfType<BadRequestObjectResult>();
        }

        [Fact]
        public void UpdateVisibility_HappyPath_ReturnsOk()
        {
            var user = EntityBuilder.NewAccount();
            ControllerContextHelper.Attach(_controller, user);
            _service.Setup(s => s.UpdateVisibility(
                    "p1", PlaylistVisibility.Public, user.Id.ToString(), false))
                .Returns(new PlaylistResponse { Visibility = PlaylistVisibility.Public });

            var result = _controller.UpdateVisibility(
                "p1", new UpdatePlaylistVisibilityRequest { Visibility = PlaylistVisibility.Public });

            result.Result.Should().BeOfType<OkObjectResult>();
        }

        [Fact]
        public void UpdateVisibility_NonOwner_ReturnsBadRequest()
        {
            var user = EntityBuilder.NewAccount();
            ControllerContextHelper.Attach(_controller, user);
            _service.Setup(s => s.UpdateVisibility(
                    It.IsAny<string>(), It.IsAny<PlaylistVisibility>(),
                    It.IsAny<string>(), It.IsAny<bool>()))
                .Throws(new AppException("forbidden"));

            var result = _controller.UpdateVisibility(
                "p1", new UpdatePlaylistVisibilityRequest { Visibility = PlaylistVisibility.Public });

            result.Result.Should().BeOfType<BadRequestObjectResult>();
        }
    }
}
