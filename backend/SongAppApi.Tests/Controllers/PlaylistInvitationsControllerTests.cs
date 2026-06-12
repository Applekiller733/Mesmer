using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Moq;
using SongAppApi.Controllers;
using SongAppApi.Models.Playlist;
using SongAppApi.Models.PlaylistInvitations;
using SongAppApi.Services;
using SongAppApi.Tests.Common;
using Xunit;

namespace SongAppApi.Tests.Controllers
{
    public class PlaylistInvitationsControllerTests
    {
        private readonly Mock<IPlaylistInvitationService> _service = new();
        private readonly PlaylistInvitationsController _controller;

        public PlaylistInvitationsControllerTests()
        {
            _controller = new PlaylistInvitationsController(_service.Object);
        }


        [Fact]
        public void Invite_NoAccount_ReturnsUnauthorized()
        {
            ControllerContextHelper.Attach(_controller, account: null);
            var result = _controller.Invite("p1", Guid.NewGuid().ToString());
            result.Result.Should().BeOfType<UnauthorizedResult>();
        }

        [Fact]
        public void Invite_HappyPath_ReturnsOk()
        {
            var user = EntityBuilder.NewAccount();
            ControllerContextHelper.Attach(_controller, user);
            var receiverId = Guid.NewGuid().ToString();
            _service.Setup(s => s.Invite(user.Id.ToString(), "p1", receiverId))
                .Returns(new PlaylistInvitationResponse { Id = "i1" });

            var result = _controller.Invite("p1", receiverId);

            result.Result.Should().BeOfType<OkObjectResult>();
        }

        [Fact]
        public void Invite_MissingPlaylist_Returns404()
        {
            var user = EntityBuilder.NewAccount();
            ControllerContextHelper.Attach(_controller, user);
            _service.Setup(s => s.Invite(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
                .Throws(new KeyNotFoundException("not found"));

            var result = _controller.Invite("p1", Guid.NewGuid().ToString());

            result.Result.Should().BeOfType<NotFoundObjectResult>();
        }


        [Fact]
        public void Accept_HappyPath_ReturnsOk()
        {
            var user = EntityBuilder.NewAccount();
            ControllerContextHelper.Attach(_controller, user);
            _service.Setup(s => s.Accept(user.Id.ToString(), "i1"))
                .Returns(new PlaylistResponse { Id = "p1" });

            var result = _controller.Accept("i1");

            result.Result.Should().BeOfType<OkObjectResult>();
        }

        [Fact]
        public void Accept_Missing_Returns404()
        {
            var user = EntityBuilder.NewAccount();
            ControllerContextHelper.Attach(_controller, user);
            _service.Setup(s => s.Accept(It.IsAny<string>(), It.IsAny<string>()))
                .Throws(new KeyNotFoundException("missing"));

            var result = _controller.Accept("i1");

            result.Result.Should().BeOfType<NotFoundObjectResult>();
        }


        [Fact]
        public void Decline_HappyPath_ReturnsOk()
        {
            var user = EntityBuilder.NewAccount();
            ControllerContextHelper.Attach(_controller, user);

            var result = _controller.Decline("i1");

            result.Should().BeOfType<OkObjectResult>();
            _service.Verify(s => s.Decline(user.Id.ToString(), "i1"), Times.Once);
        }


        [Fact]
        public void Cancel_HappyPath_ReturnsOk()
        {
            var user = EntityBuilder.NewAccount();
            ControllerContextHelper.Attach(_controller, user);

            var result = _controller.Cancel("i1");

            result.Should().BeOfType<OkObjectResult>();
            _service.Verify(s => s.Cancel(user.Id.ToString(), "i1"), Times.Once);
        }

        [Fact]
        public void Cancel_Missing_Returns404()
        {
            var user = EntityBuilder.NewAccount();
            ControllerContextHelper.Attach(_controller, user);
            _service.Setup(s => s.Cancel(It.IsAny<string>(), It.IsAny<string>()))
                .Throws(new KeyNotFoundException("missing"));

            var result = _controller.Cancel("i1");

            result.Should().BeOfType<NotFoundObjectResult>();
        }


        [Fact]
        public void GetIncoming_HappyPath_ReturnsOk()
        {
            var user = EntityBuilder.NewAccount();
            ControllerContextHelper.Attach(_controller, user);
            _service.Setup(s => s.GetIncoming(user.Id.ToString()))
                .Returns(new List<PlaylistInvitationResponse>());

            _controller.GetIncoming().Result.Should().BeOfType<OkObjectResult>();
        }

        [Fact]
        public void GetOutgoing_HappyPath_ReturnsOk()
        {
            var user = EntityBuilder.NewAccount();
            ControllerContextHelper.Attach(_controller, user);
            _service.Setup(s => s.GetOutgoing(user.Id.ToString()))
                .Returns(new List<PlaylistInvitationResponse>());

            _controller.GetOutgoing().Result.Should().BeOfType<OkObjectResult>();
        }

        [Fact]
        public void GetIncomingCount_ReturnsCount()
        {
            var user = EntityBuilder.NewAccount();
            ControllerContextHelper.Attach(_controller, user);
            _service.Setup(s => s.CountIncoming(user.Id.ToString())).Returns(4);

            var result = _controller.GetIncomingCount();

            result.Result.Should().BeOfType<OkObjectResult>();
        }
    }
}
