using Microsoft.AspNetCore.Mvc;
using SongAppApi.Authorization;
using SongAppApi.Services;

namespace SongAppApi.Controllers
{
    [Authorization.Authorize]
    [ApiController]
    [Route("[controller]")]
    public class RecommendationsController : BaseController
    {
        private readonly IRecommendationService _service;

        public RecommendationsController(IRecommendationService service)
        {
            _service = service;
        }

        /// <summary>
        /// Returns up to topK recommended song IDs based on the given
        /// playlist's content. The frontend uses this to drive the For You
        /// carousel when a user selects a playlist for recommendations.
        /// </summary>
        /// <param name="playlistId">UUID of the playlist to base recommendations on.</param>
        /// <param name="topK">How many recommendations to return (default 5, max 50).</param>
        [HttpGet("for-playlist/{playlistId}")]
        public async Task<ActionResult<IEnumerable<string>>> ForPlaylist(
            string playlistId,
            [FromQuery] int topK = 5,
            CancellationToken ct = default)
        {
            // Defensive cap. The Python service is happy to return more,
            // but the carousel can't render thousands of slides usefully.
            topK = Math.Clamp(topK, 1, 50);

            try
            {
                var ids = await _service.GetRecommendationsForPlaylistAsync(
                    playlistId, topK, ct);
                return Ok(ids);
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { message = ex.Message });
            }
        }
    }
}