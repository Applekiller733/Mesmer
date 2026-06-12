using System.Net;
using System.Net.Http.Json;
using System.Text;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using SongAppApi.Helpers;
using SongAppApi.Models.Accounts;
using SongAppApi.Models.Playlist;
using SongAppApi.Models.Songs;
using SongAppApi.Services;
using Xunit;

namespace SongAppApi.Tests.Services
{
    public class RecommendationServiceTests
    {
        private static PlaylistResponse SamplePlaylist(int songCount = 2)
        {
            var songs = new List<SongResponse>();
            for (int i = 0; i < songCount; i++)
            {
                songs.Add(new SongResponse
                {
                    Id = Guid.NewGuid().ToString(),
                    Name = $"Song {i}",
                    Artist = $"Artist {i}",
                });
            }
            return new PlaylistResponse
            {
                Id = Guid.NewGuid().ToString(),
                Name = "Test",
                Songs = songs,
                CreatedBy = new AccountResponse { Id = Guid.NewGuid().ToString() },
            };
        }

        private static (RecommendationService service, Mock<IPlaylistService> playlistService, Func<HttpRequestMessage> lastRequest)
            BuildService(HttpStatusCode status, string responseBody)
        {
            var handler = new StubHandler(status, responseBody);
            var http = new HttpClient(handler) { BaseAddress = new Uri("http://localhost:8000") };
            var playlistService = new Mock<IPlaylistService>();
            var service = new RecommendationService(
                http, playlistService.Object, NullLogger<RecommendationService>.Instance);
            return (service, playlistService, () => handler.LastRequest!);
        }

        [Fact]
        public async Task GetRecommendations_HappyPath_ReturnsIdsTakingTopK()
        {
            var playlist = SamplePlaylist(songCount: 2);
            var responseBody = """{"recommendedIds":["a","b","c","d","e"]}""";
            var (service, playlistSvc, _) = BuildService(HttpStatusCode.OK, responseBody);
            playlistSvc.Setup(p => p.GetInternal(It.IsAny<string>())).Returns(playlist);

            var result = await service.GetRecommendationsForPlaylistAsync(
                playlist.Id, topK: 3);

            result.Should().BeEquivalentTo(new[] { "a", "b", "c" });
        }

        [Fact]
        public async Task GetRecommendations_HttpRequestException_ThrowsAppException()
        {
            var playlist = SamplePlaylist();
            var handler = new ThrowingHandler(new HttpRequestException("connection refused"));
            var http = new HttpClient(handler) { BaseAddress = new Uri("http://localhost:8000") };
            var playlistSvc = new Mock<IPlaylistService>();
            playlistSvc.Setup(p => p.GetInternal(It.IsAny<string>())).Returns(playlist);
            var service = new RecommendationService(
                http, playlistSvc.Object, NullLogger<RecommendationService>.Instance);

            var act = () => service.GetRecommendationsForPlaylistAsync(playlist.Id, topK: 5);

            await act.Should().ThrowAsync<AppException>()
                .WithMessage("*unavailable*");
        }

        [Fact]
        public async Task GetRecommendations_Non200_ThrowsAppException()
        {
            var playlist = SamplePlaylist();
            var (service, playlistSvc, _) = BuildService(HttpStatusCode.InternalServerError, "boom");
            playlistSvc.Setup(p => p.GetInternal(It.IsAny<string>())).Returns(playlist);

            var act = () => service.GetRecommendationsForPlaylistAsync(playlist.Id, topK: 5);

            await act.Should().ThrowAsync<AppException>()
                .WithMessage("*500*");
        }

        [Fact]
        public async Task GetRecommendations_EmptyRecommendedIds_ReturnsEmpty()
        {
            var playlist = SamplePlaylist();
            var (service, playlistSvc, _) = BuildService(
                HttpStatusCode.OK, """{"recommendedIds":[]}""");
            playlistSvc.Setup(p => p.GetInternal(It.IsAny<string>())).Returns(playlist);

            var result = await service.GetRecommendationsForPlaylistAsync(playlist.Id, topK: 5);

            result.Should().BeEmpty();
        }

        [Fact]
        public async Task GetRecommendations_NullRecommendedIds_ReturnsEmpty()
        {
            var playlist = SamplePlaylist();
            var (service, playlistSvc, _) = BuildService(
                HttpStatusCode.OK, """{}""");
            playlistSvc.Setup(p => p.GetInternal(It.IsAny<string>())).Returns(playlist);

            var result = await service.GetRecommendationsForPlaylistAsync(playlist.Id, topK: 5);

            result.Should().BeEmpty();
        }

        [Fact]
        public async Task GetRecommendations_PostsToRecommendEndpoint()
        {
            var playlist = SamplePlaylist(songCount: 3);
            var (service, playlistSvc, lastRequestAccessor) = BuildService(
                HttpStatusCode.OK, """{"recommendedIds":["x"]}""");
            playlistSvc.Setup(p => p.GetInternal(It.IsAny<string>())).Returns(playlist);

            await service.GetRecommendationsForPlaylistAsync(playlist.Id, topK: 5);

            var req = lastRequestAccessor();
            req.Method.Should().Be(HttpMethod.Post);
            req.RequestUri!.AbsolutePath.Should().Be("/recommend-ids");
        }

        [Fact]
        public async Task GetRecommendations_RequestBodyContainsPlaylistData()
        {
            var playlist = SamplePlaylist(songCount: 2);
            var (service, playlistSvc, lastRequestAccessor) = BuildService(
                HttpStatusCode.OK, """{"recommendedIds":[]}""");
            playlistSvc.Setup(p => p.GetInternal(It.IsAny<string>())).Returns(playlist);

            await service.GetRecommendationsForPlaylistAsync(playlist.Id, topK: 5);

            var req = lastRequestAccessor();
            var body = await req.Content!.ReadAsStringAsync();
            body.Should().Contain(playlist.Id);
            body.Should().Contain(playlist.Songs[0].Name);
        }


        private class StubHandler : HttpMessageHandler
        {
            private readonly HttpStatusCode _status;
            private readonly string _body;
            public HttpRequestMessage? LastRequest;

            public StubHandler(HttpStatusCode status, string body)
            {
                _status = status;
                _body = body;
            }

            protected override Task<HttpResponseMessage> SendAsync(
                HttpRequestMessage request, CancellationToken cancellationToken)
            {
                LastRequest = request;
                return Task.FromResult(new HttpResponseMessage(_status)
                {
                    Content = new StringContent(_body, Encoding.UTF8, "application/json")
                });
            }
        }

        private class ThrowingHandler : HttpMessageHandler
        {
            private readonly Exception _exception;
            public ThrowingHandler(Exception exception) => _exception = exception;
            protected override Task<HttpResponseMessage> SendAsync(
                HttpRequestMessage request, CancellationToken cancellationToken)
            {
                throw _exception;
            }
        }
    }
}
