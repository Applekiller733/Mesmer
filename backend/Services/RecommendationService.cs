using System.Net.Http.Json;
using SongAppApi.Helpers;
using SongAppApi.Models.Songs;
using SongAppApi.Models.Recommendations;

namespace SongAppApi.Services
{
    public interface IRecommendationService
    {
        Task<IEnumerable<string>> GetRecommendationsForPlaylistAsync(
            string playlistId, int topK, CancellationToken ct = default);
    }

    public class RecommendationService : IRecommendationService
    {
        private readonly HttpClient _http;
        private readonly IPlaylistService _playlistService;
        private readonly ILogger<RecommendationService> _logger;

        public RecommendationService(
            HttpClient http,
            IPlaylistService playlistService,
            ILogger<RecommendationService> logger)
        {
            _http = http;
            _playlistService = playlistService;
            _logger = logger;
        }

        public async Task<IEnumerable<string>> GetRecommendationsForPlaylistAsync(
            string playlistId, int topK, CancellationToken ct = default)
        {
            var playlist = _playlistService.Get(playlistId);
            if (playlist == null)
                throw new KeyNotFoundException($"Playlist {playlistId} not found.");

            
            var requestBody = new
            {
                Id = playlist.Id.ToString(),
                Songs = playlist.Songs.Select(s => new
                {
                    Id = s.Id.ToString(),
                    Name = s.Name,
                    Artist = s.Artist,
                }).ToList(),
            };

            HttpResponseMessage resp;
            try
            {
                resp = await _http.PostAsJsonAsync(
                    "/recommend-ids", requestBody, ct);
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError(ex,
                    "Recommendation service unreachable at {BaseAddress}",
                    _http.BaseAddress);
                throw new AppException(
                    "Recommendation service unavailable. Please try again later.");
            }

            if (!resp.IsSuccessStatusCode)
            {
                var body = await resp.Content.ReadAsStringAsync(ct);
                _logger.LogWarning(
                    "Recommendation service returned {Status}: {Body}",
                    resp.StatusCode, body);
                throw new AppException(
                    $"Recommendation service error ({(int)resp.StatusCode}).");
            }

            var payload = await resp.Content
                .ReadFromJsonAsync<RecommendationResponse>(cancellationToken: ct);

            if (payload?.RecommendedIds == null)
                return Enumerable.Empty<string>();

            return payload.RecommendedIds.Take(topK);
        }
    }
}