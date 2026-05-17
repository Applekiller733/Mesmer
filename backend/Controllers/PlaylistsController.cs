using Microsoft.AspNetCore.Mvc;
using SongAppApi.Authorization;
using SongAppApi.Entities;
using SongAppApi.Helpers.Enumerators;
using SongAppApi.Models.Playlist;
using SongAppApi.Services;

namespace SongAppApi.Controllers
{
    [Authorization.Authorize]
    [ApiController]
    [Route("[controller]")]
    public class PlaylistsController : BaseController
    {
        private IPlaylistService _service;

        public PlaylistsController(IPlaylistService service)
        {
            _service = service;
        }

        [HttpGet("{id}")]
        public ActionResult<PlaylistResponse> Get(string id)
        {
            try
            {
                if (Account == null) return Unauthorized();
                // Visibility enforcement happens inside the service. If the
                // current user isn't entitled to see this playlist, the
                // service throws KeyNotFoundException — we surface that as
                // 404, intentionally indistinguishable from a truly missing
                // playlist so private IDs can't be enumerated.
                var response = _service.Get(id, Account.Id.ToString());
                return Ok(response);
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        // Admin-only firehose. The previous version of this endpoint
        // returned every playlist regardless of visibility, which would
        // leak Private playlists once Visibility was introduced. Gating
        // to Admin preserves the surface area for admin tooling and
        // locks it down for everyone else. A user-facing "browse public
        // playlists" feed, if/when we want it, gets its own endpoint.
        [Authorization.Authorize(Role.Admin)]
        [HttpGet]
        public ActionResult<IEnumerable<PlaylistResponse>> GetAll()
        {
            try
            {
                var response = _service.GetAll();
                return Ok(response);
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, new { message = ex.Message });
            }
        }

        [HttpGet("made-by/{accountid}")]
        public ActionResult<IEnumerable<PlaylistResponse>> GetAllCreatedByAccount(string accountid)
        {
            try
            {
                if (Account == null) return Unauthorized();
                // Service filters by visibility relative to the viewer —
                // self-view returns everything, other-view returns only
                // Public. See PlaylistService.GetCreatedByAccount.
                var response = _service.GetCreatedByAccount(accountid, Account.Id.ToString());
                return Ok(response);
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpGet("saved-by/{accountid}")]
        public ActionResult<IEnumerable<PlaylistResponse>> GetAllSavedByAccount(string accountid)
        {
            try
            {
                if (Account == null) return Unauthorized();
                var response = _service.GetSavedByAccount(
                    accountid,
                    Account.Id.ToString(),
                    Account.Role == Role.Admin);
                return Ok(response);
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpPost("create-playlist")]
        public ActionResult<PlaylistResponse> CreatePlaylist(CreatePlaylistRequest request)
        {
            try
            {
                if (Account == null) return Unauthorized();
                var response = _service.Create(request, Account);
                return Ok(response);
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        //todo remove the id from the URL and instead add to request?
        [HttpPut]
        public ActionResult<PlaylistResponse> UpdatePlaylist(UpdatePlaylistRequest request)
        {
            try
            {
                if (Account == null) return Unauthorized();
                // Owner/admin authority is now enforced inside the service.
                // The previous controller-side check was buggy anyway —
                // `playlist.CreatedBy.Id != Account.Id.ToString() ||
                //  Account.Role != Role.Admin` returns true for any
                // non-admin owner, blocking legitimate edits.
                var response = _service.Update(
                    request.Id,
                    request,
                    Account.Id.ToString(),
                    Account.Role == Role.Admin);
                return Ok(response);
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpDelete]
        public ActionResult Delete(DeletePlaylistRequest request)
        {
            try
            {
                if (Account == null) return Unauthorized();
                _service.Delete(
                    request.Id,
                    Account.Id.ToString(),
                    Account.Role == Role.Admin);
                return Ok(new { message = "Playlist successfully deleted" });
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        /// <summary>
        /// Add the playlist to the current user's saved library. Public
        /// playlists are saveable by anyone; Unlisted only by users with
        /// a pending invitation (in which case this also clears the
        /// invitation row). Returns the freshly-saved playlist.
        /// </summary>
        [HttpPost("{id}/save")]
        public ActionResult<PlaylistResponse> Save(string id)
        {
            try
            {
                if (Account == null) return Unauthorized();
                var response = _service.Save(id, Account.Id.ToString());
                return Ok(response);
            }
            catch (KeyNotFoundException ex)
            {
                // Includes both "doesn't exist" and "can't see it" —
                // both return 404, deliberately indistinguishable.
                return NotFound(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        /// <summary>
        /// Remove the playlist from the current user's saved library.
        /// The owner cannot unsave their own creation; the service
        /// rejects that with an explicit error.
        /// </summary>
        [HttpDelete("{id}/save")]
        public ActionResult Unsave(string id)
        {
            try
            {
                if (Account == null) return Unauthorized();
                _service.Unsave(id, Account.Id.ToString());
                return Ok(new { message = "Playlist removed from your library." });
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        /// <summary>
        /// Owner (or admin) changes the playlist's visibility. See
        /// PlaylistService.UpdateVisibility for the side-effect rules
        /// (Public/Unlisted ↔ Private transitions can cancel pending
        /// invitations; existing saves are never stripped).
        /// </summary>
        [HttpPatch("{id}/visibility")]
        public ActionResult<PlaylistResponse> UpdateVisibility(
            string id, [FromBody] UpdatePlaylistVisibilityRequest request)
        {
            try
            {
                if (Account == null) return Unauthorized();
                var response = _service.UpdateVisibility(
                    id,
                    request.Visibility,
                    Account.Id.ToString(),
                    Account.Role == Role.Admin);
                return Ok(response);
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }
    }
}