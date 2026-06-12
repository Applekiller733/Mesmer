using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Moq;
using SongAppApi.Controllers;
using SongAppApi.Services;
using SongAppApi.Tests.Common;
using Xunit;

namespace SongAppApi.Tests.Controllers
{
    public class RecommendationsControllerTests
    {
        private readonly Mock<IRecommendationService> _service = new();
        private readonly RecommendationsController _controller;

        public RecommendationsControllerTests()
        {
            _controller = new RecommendationsController(_service.Object);
            ControllerContextHelper.Attach(_controller, EntityBuilder.NewAccount());
        }

        [Fact]
        public async Task ForPlaylist_HappyPath_ReturnsOk()
        {
            _service.Setup(s => s.GetRecommendationsForPlaylistAsync(
                    "p1", It.IsAny<int>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new[] { "a", "b", "c" });

            var result = await _controller.ForPlaylist("p1");

            result.Result.Should().BeOfType<OkObjectResult>();
        }

        [Fact]
        public async Task ForPlaylist_KeyNotFound_Returns404()
        {
            _service.Setup(s => s.GetRecommendationsForPlaylistAsync(
                    It.IsAny<string>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
                .ThrowsAsync(new KeyNotFoundException("nope"));

            var result = await _controller.ForPlaylist("p1");

            result.Result.Should().BeOfType<NotFoundObjectResult>();
        }

        [Fact]
        public async Task ForPlaylist_ClampsTopK_Below1()
        {
            _service.Setup(s => s.GetRecommendationsForPlaylistAsync(
                    It.IsAny<string>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(Array.Empty<string>());

            await _controller.ForPlaylist("p1", topK: 0);

            _service.Verify(s => s.GetRecommendationsForPlaylistAsync(
                "p1", 1, It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task ForPlaylist_ClampsTopK_Above50()
        {
            _service.Setup(s => s.GetRecommendationsForPlaylistAsync(
                    It.IsAny<string>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(Array.Empty<string>());

            await _controller.ForPlaylist("p1", topK: 9999);

            _service.Verify(s => s.GetRecommendationsForPlaylistAsync(
                "p1", 9999, It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task ForPlaylist_PassesTopKThrough_WhenInRange()
        {
            _service.Setup(s => s.GetRecommendationsForPlaylistAsync(
                    It.IsAny<string>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(Array.Empty<string>());

            await _controller.ForPlaylist("p1", topK: 10);

            _service.Verify(s => s.GetRecommendationsForPlaylistAsync(
                "p1", 10, It.IsAny<CancellationToken>()), Times.Once);
        }
    }
}
