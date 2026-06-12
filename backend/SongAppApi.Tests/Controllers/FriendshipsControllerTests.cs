using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Moq;
using SongAppApi.Controllers;
using SongAppApi.Helpers;
using SongAppApi.Helpers.Enumerators;
using SongAppApi.Models.Friendships;
using SongAppApi.Services;
using SongAppApi.Tests.Common;
using Xunit;

namespace SongAppApi.Tests.Controllers
{
    public class FriendshipsControllerTests
    {
        private readonly Mock<IFriendshipService> _service = new();
        private readonly FriendshipsController _controller;

        public FriendshipsControllerTests()
        {
            _controller = new FriendshipsController(_service.Object);
        }


        [Fact]
        public void SendRequest_NoAccount_ReturnsUnauthorized()
        {
            ControllerContextHelper.Attach(_controller, account: null);
            var result = _controller.SendRequest(Guid.NewGuid().ToString());
            result.Result.Should().BeOfType<UnauthorizedResult>();
        }

        [Fact]
        public void SendRequest_HappyPath_ReturnsOk()
        {
            var user = EntityBuilder.NewAccount();
            ControllerContextHelper.Attach(_controller, user);
            var otherId = Guid.NewGuid().ToString();
            _service.Setup(s => s.SendRequest(user.Id.ToString(), otherId))
                .Returns(new FriendshipResponse { Id = "f1", Status = FriendshipStatus.Pending });

            var result = _controller.SendRequest(otherId);

            result.Result.Should().BeOfType<OkObjectResult>();
        }

        [Fact]
        public void SendRequest_KeyNotFound_Returns404()
        {
            var user = EntityBuilder.NewAccount();
            ControllerContextHelper.Attach(_controller, user);
            _service.Setup(s => s.SendRequest(It.IsAny<string>(), It.IsAny<string>()))
                .Throws(new KeyNotFoundException("missing"));

            var result = _controller.SendRequest(Guid.NewGuid().ToString());

            result.Result.Should().BeOfType<NotFoundObjectResult>();
        }


        [Fact]
        public void Accept_HappyPath_ReturnsOk()
        {
            var user = EntityBuilder.NewAccount();
            ControllerContextHelper.Attach(_controller, user);
            _service.Setup(s => s.AcceptRequest(user.Id.ToString(), "f1"))
                .Returns(new FriendshipResponse { Id = "f1", Status = FriendshipStatus.Accepted });

            var result = _controller.Accept("f1");

            result.Result.Should().BeOfType<OkObjectResult>();
        }

        [Fact]
        public void Decline_HappyPath_ReturnsOk()
        {
            var user = EntityBuilder.NewAccount();
            ControllerContextHelper.Attach(_controller, user);

            var result = _controller.Decline("f1");

            result.Should().BeOfType<OkObjectResult>();
            _service.Verify(s => s.DeclineRequest(user.Id.ToString(), "f1"), Times.Once);
        }


        [Fact]
        public void RemoveFriend_HappyPath_ReturnsOk()
        {
            var user = EntityBuilder.NewAccount();
            ControllerContextHelper.Attach(_controller, user);
            var otherId = Guid.NewGuid().ToString();

            var result = _controller.RemoveFriend(otherId);

            result.Should().BeOfType<OkObjectResult>();
            _service.Verify(s => s.RemoveFriend(user.Id.ToString(), otherId), Times.Once);
        }

        [Fact]
        public void Block_HappyPath_ReturnsOk()
        {
            var user = EntityBuilder.NewAccount();
            ControllerContextHelper.Attach(_controller, user);
            var otherId = Guid.NewGuid().ToString();
            _service.Setup(s => s.Block(user.Id.ToString(), otherId))
                .Returns(new FriendshipResponse { Status = FriendshipStatus.Blocked });

            var result = _controller.Block(otherId);

            result.Result.Should().BeOfType<OkObjectResult>();
        }

        [Fact]
        public void Unblock_HappyPath_ReturnsOk()
        {
            var user = EntityBuilder.NewAccount();
            ControllerContextHelper.Attach(_controller, user);
            var otherId = Guid.NewGuid().ToString();

            var result = _controller.Unblock(otherId);

            result.Should().BeOfType<OkObjectResult>();
            _service.Verify(s => s.Unblock(user.Id.ToString(), otherId), Times.Once);
        }


        [Fact]
        public void GetRelationship_HappyPath_ReturnsOk()
        {
            var user = EntityBuilder.NewAccount();
            ControllerContextHelper.Attach(_controller, user);
            var otherId = Guid.NewGuid().ToString();
            _service.Setup(s => s.GetRelationship(user.Id.ToString(), otherId))
                .Returns(new RelationshipStatusResponse());

            var result = _controller.GetRelationship(otherId);

            result.Result.Should().BeOfType<OkObjectResult>();
        }

        [Fact]
        public void GetFriends_NoAccount_ReturnsUnauthorized()
        {
            ControllerContextHelper.Attach(_controller, account: null);
            var result = _controller.GetFriends();
            result.Result.Should().BeOfType<UnauthorizedResult>();
        }

        [Fact]
        public void GetFriends_HappyPath_ReturnsOk()
        {
            var user = EntityBuilder.NewAccount();
            ControllerContextHelper.Attach(_controller, user);
            _service.Setup(s => s.GetFriends(user.Id.ToString()))
                .Returns(new List<FriendshipResponse>());

            var result = _controller.GetFriends();

            result.Result.Should().BeOfType<OkObjectResult>();
        }

        [Fact]
        public void GetIncoming_HappyPath_ReturnsOk()
        {
            var user = EntityBuilder.NewAccount();
            ControllerContextHelper.Attach(_controller, user);
            _service.Setup(s => s.GetIncomingRequests(user.Id.ToString()))
                .Returns(new List<FriendshipResponse>());

            _controller.GetIncoming().Result.Should().BeOfType<OkObjectResult>();
        }

        [Fact]
        public void GetOutgoing_HappyPath_ReturnsOk()
        {
            var user = EntityBuilder.NewAccount();
            ControllerContextHelper.Attach(_controller, user);
            _service.Setup(s => s.GetOutgoingRequests(user.Id.ToString()))
                .Returns(new List<FriendshipResponse>());

            _controller.GetOutgoing().Result.Should().BeOfType<OkObjectResult>();
        }

        [Fact]
        public void GetBlocked_HappyPath_ReturnsOk()
        {
            var user = EntityBuilder.NewAccount();
            ControllerContextHelper.Attach(_controller, user);
            _service.Setup(s => s.GetBlocked(user.Id.ToString()))
                .Returns(new List<FriendshipResponse>());

            _controller.GetBlocked().Result.Should().BeOfType<OkObjectResult>();
        }

        [Fact]
        public void GetIncomingCount_ReturnsCount()
        {
            var user = EntityBuilder.NewAccount();
            ControllerContextHelper.Attach(_controller, user);
            _service.Setup(s => s.CountIncomingRequests(user.Id.ToString())).Returns(7);

            var result = _controller.GetIncomingCount();

            result.Result.Should().BeOfType<OkObjectResult>();
        }
    }
}
