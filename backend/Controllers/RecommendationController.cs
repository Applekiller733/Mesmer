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

        [HttpGet("for-playlist/{playlistId}")]
        public async Task<ActionResult<IEnumerable<string>>> ForPlaylist(
            string playlistId,
            [FromQuery] int topK = 5,
            CancellationToken ct = default)
        {
            topK = Math.Max(1, topK);

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