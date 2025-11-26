using Microsoft.AspNetCore.Mvc;
using SongAppApi.Entities;
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
                var response = _service.Get(id);
                return Ok(response);
            }
            catch (Exception ex)
            {
                return NotFound(new { message = ex.Message });
            }
        }

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
                var response = _service.GetCreatedByAccount(accountid);
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
                var response = _service.GetSavedByAccount(accountid);
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
                //todo test if it works
                var response = _service.Create(request, Account);
                return Ok(response);
            }
            catch(Exception ex)
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
                var playlist = _service.Get(request.Id);
                if (playlist.CreatedBy.Id != Account.Id.ToString() || Account.Role != Role.Admin)
                    return Unauthorized(new { message = "Unauthorized" });

                var response = _service.Update(request.Id, request);
                return Ok(response);
            }
            catch (Exception ex)
            {
                return BadRequest(new {message = ex.Message});
            }
        }

        //[HttpPost("flip-is-public")]
        //public ActionResult FlipIsPublic()
        //{
        //    try
        //    {
                
        //    }
        //    catch (Exception ex)
        //    {
        //        return BadRequest(new { message = ex.Message });
        //    }
        //}

        [HttpDelete]
        public ActionResult Delete(DeletePlaylistRequest request)
        {
            try
            {
                var playlist = _service.Get(request.Id);
                if (playlist.CreatedBy.Id != Account.Id.ToString() || Account.Role != Role.Admin)
                    return Unauthorized(new { message = "Unauthorized" });

                _service.Delete(request.Id);
                return Ok(new { message = "Playlist successfully deleted" });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }
    }
}
