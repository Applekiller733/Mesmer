using Microsoft.AspNetCore.Mvc;
using SongAppApi.Models.Playlist;
using SongAppApi.Models.PlaylistInvitations;
using SongAppApi.Services;

namespace SongAppApi.Controllers
{ 
    [Authorization.Authorize]
    [ApiController]
    [Route("playlist-invitations")]
    public class PlaylistInvitationsController : BaseController
    {
        private readonly IPlaylistInvitationService _service;

        public PlaylistInvitationsController(IPlaylistInvitationService service)
        {
            _service = service;
        }

        // -------------------- State changes --------------------

        /// <summary>
        /// Send a playlist invitation. Authority depends on the
        /// playlist's visibility; see PlaylistInvitationService.Invite
        /// for the full rules.
        /// </summary>
        [HttpPost("invite/{playlistId}/{userId}")]
        public ActionResult<PlaylistInvitationResponse> Invite(string playlistId, string userId)
        {
            try
            {
                if (Account == null) return Unauthorized();
                var response = _service.Invite(Account.Id.ToString(), playlistId, userId);
                return Ok(response);
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { message = ex.Message });
            }
            // AppException → 400 via the global error handler middleware
            // (same path the FriendshipsController relies on).
        }

        /// <summary>
        /// Accept an incoming invitation. Returns the saved playlist.
        /// </summary>
        [HttpPost("{id}/accept")]
        public ActionResult<PlaylistResponse> Accept(string id)
        {
            try
            {
                if (Account == null) return Unauthorized();
                var response = _service.Accept(Account.Id.ToString(), id);
                return Ok(response);
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { message = ex.Message });
            }
        }

        /// <summary>
        /// Decline an incoming invitation. Only the receiver can decline.
        /// </summary>
        [HttpPost("{id}/decline")]
        public ActionResult Decline(string id)
        {
            try
            {
                if (Account == null) return Unauthorized();
                _service.Decline(Account.Id.ToString(), id);
                return Ok(new { message = "Invitation declined." });
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { message = ex.Message });
            }
        }

        /// <summary>
        /// Sender retracts their own outgoing invitation before it's been
        /// accepted or declined. Different from Decline only in who's
        /// authorised.
        /// </summary>
        [HttpDelete("{id}")]
        public ActionResult Cancel(string id)
        {
            try
            {
                if (Account == null) return Unauthorized();
                _service.Cancel(Account.Id.ToString(), id);
                return Ok(new { message = "Invitation cancelled." });
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { message = ex.Message });
            }
        }

        // -------------------- Queries --------------------

        /// <summary>
        /// Pending invitations sent to the current user. Backs the
        /// "Playlist Shares" panel in the notification popper and the
        /// Playlists tab on the Socials page.
        /// </summary>
        [HttpGet("incoming")]
        public ActionResult<IEnumerable<PlaylistInvitationResponse>> GetIncoming()
        {
            if (Account == null) return Unauthorized();
            return Ok(_service.GetIncoming(Account.Id.ToString()));
        }

        /// <summary>
        /// Pending invitations the current user has sent. For the
        /// "I shared this with X" outbox view.
        /// </summary>
        [HttpGet("outgoing")]
        public ActionResult<IEnumerable<PlaylistInvitationResponse>> GetOutgoing()
        {
            if (Account == null) return Unauthorized();
            return Ok(_service.GetOutgoing(Account.Id.ToString()));
        }

        /// <summary>
        /// Count of pending invitations in the current user's inbox.
        /// Cheap query — used by the navbar badge so we don't have to
        /// pull the full inbox on each poll.
        /// </summary>
        [HttpGet("incoming/count")]
        public ActionResult<object> GetIncomingCount()
        {
            if (Account == null) return Unauthorized();
            var count = _service.CountIncoming(Account.Id.ToString());
            return Ok(new { count });
        }
    }
}