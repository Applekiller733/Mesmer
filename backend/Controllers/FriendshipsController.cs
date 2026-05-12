using Microsoft.AspNetCore.Mvc;
using SongAppApi.Authorization;
using SongAppApi.Models.Friendships;
using SongAppApi.Services;

namespace SongAppApi.Controllers
{
    /// <summary>
    /// Endpoints for the friend-system feature. The route design uses
    /// action-style URLs (e.g. /friendships/{id}/accept) rather than
    /// pure REST verbs. The friend-system has too many distinct actions
    /// — accept, decline, block, unblock — to map cleanly onto PATCH
    /// or PUT, and naming each action explicitly is clearer for the
    /// frontend.
    /// </summary>
    [Authorization.Authorize]
    [ApiController]
    [Route("[controller]")]
    public class FriendshipsController : BaseController
    {
        private readonly IFriendshipService _service;

        public FriendshipsController(IFriendshipService service)
        {
            _service = service;
        }

        // -------------------- State changes --------------------

        /// <summary>
        /// Send a friend request to another user.
        /// </summary>
        [HttpPost("request/{userId}")]
        public ActionResult<FriendshipResponse> SendRequest(string userId)
        {
            try
            {
                if (Account == null) return Unauthorized();
                var response = _service.SendRequest(Account.Id.ToString(), userId);
                return Ok(response);
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { message = ex.Message });
            }
            // AppException is handled by ErrorHandlerMiddleware (400 with message).
        }

        /// <summary>
        /// Accept an incoming friend request. Only the receiver can accept.
        /// </summary>
        [HttpPost("{id}/accept")]
        public ActionResult<FriendshipResponse> Accept(string id)
        {
            try
            {
                if (Account == null) return Unauthorized();
                var response = _service.AcceptRequest(Account.Id.ToString(), id);
                return Ok(response);
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { message = ex.Message });
            }
        }

        /// <summary>
        /// Decline an incoming friend request. Only the receiver can decline.
        /// Removes the row entirely.
        /// </summary>
        [HttpPost("{id}/decline")]
        public ActionResult Decline(string id)
        {
            try
            {
                if (Account == null) return Unauthorized();
                _service.DeclineRequest(Account.Id.ToString(), id);
                return Ok(new { message = "Request declined." });
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { message = ex.Message });
            }
        }

        /// <summary>
        /// Remove a friend by their user ID. Either party can do this on
        /// an Accepted friendship.
        /// </summary>
        [HttpDelete("with/{userId}")]
        public ActionResult RemoveFriend(string userId)
        {
            try
            {
                if (Account == null) return Unauthorized();
                _service.RemoveFriend(Account.Id.ToString(), userId);
                return Ok(new { message = "Friendship removed." });
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { message = ex.Message });
            }
        }

        /// <summary>
        /// Block a user. Removes any existing relationship and inserts a
        /// Blocked row.
        /// </summary>
        [HttpPost("block/{userId}")]
        public ActionResult<FriendshipResponse> Block(string userId)
        {
            try
            {
                if (Account == null) return Unauthorized();
                var response = _service.Block(Account.Id.ToString(), userId);
                return Ok(response);
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { message = ex.Message });
            }
        }

        /// <summary>
        /// Unblock a user. Doesn't restore any prior friendship — they're
        /// just back to being a stranger.
        /// </summary>
        [HttpDelete("block/{userId}")]
        public ActionResult Unblock(string userId)
        {
            try
            {
                if (Account == null) return Unauthorized();
                _service.Unblock(Account.Id.ToString(), userId);
                return Ok(new { message = "User unblocked." });
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { message = ex.Message });
            }
        }

        // -------------------- Queries --------------------

        /// <summary>
        /// Get the relationship summary between the current user and
        /// another. Drives the profile-page button. Returns Status=null
        /// when no relationship exists OR when the other user has
        /// shadow-blocked the current user.
        /// </summary>
        [HttpGet("with/{userId}")]
        public ActionResult<RelationshipStatusResponse> GetRelationship(string userId)
        {
            try
            {
                if (Account == null) return Unauthorized();
                var response = _service.GetRelationship(Account.Id.ToString(), userId);
                return Ok(response);
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        /// <summary>
        /// All friends of the current user (Accepted relationships).
        /// </summary>
        [HttpGet("friends")]
        public ActionResult<IEnumerable<FriendshipResponse>> GetFriends()
        {
            if (Account == null) return Unauthorized();
            return Ok(_service.GetFriends(Account.Id.ToString()));
        }

        /// <summary>
        /// Pending friend requests sent to the current user.
        /// </summary>
        [HttpGet("incoming")]
        public ActionResult<IEnumerable<FriendshipResponse>> GetIncoming()
        {
            if (Account == null) return Unauthorized();
            return Ok(_service.GetIncomingRequests(Account.Id.ToString()));
        }

        /// <summary>
        /// Pending friend requests sent BY the current user.
        /// </summary>
        [HttpGet("outgoing")]
        public ActionResult<IEnumerable<FriendshipResponse>> GetOutgoing()
        {
            if (Account == null) return Unauthorized();
            return Ok(_service.GetOutgoingRequests(Account.Id.ToString()));
        }

        /// <summary>
        /// Users the current user has blocked.
        /// </summary>
        [HttpGet("blocked")]
        public ActionResult<IEnumerable<FriendshipResponse>> GetBlocked()
        {
            if (Account == null) return Unauthorized();
            return Ok(_service.GetBlocked(Account.Id.ToString()));
        }

        /// <summary>
        /// Count of incoming pending requests. Cheap query, used by the
        /// navbar badge to avoid pulling the full list. Polled by the
        /// frontend every minute or so (or refreshed on key events).
        /// </summary>
        [HttpGet("incoming/count")]
        public ActionResult<object> GetIncomingCount()
        {
            if (Account == null) return Unauthorized();
            var count = _service.CountIncomingRequests(Account.Id.ToString());
            return Ok(new { count });
        }
    }
}