using Microsoft.AspNetCore.Mvc;
using SongAppApi.Authorization;
using SongAppApi.Entities;
using SongAppApi.Models.Songs;
using SongAppApi.Services;

namespace SongAppApi.Controllers
{
    [Authorization.Authorize]
    [ApiController]
    [Route("[controller]")]
    public class SongsController : BaseController
    {
        ISongService _service;
        IAccountService _accountService;

        public SongsController(ISongService service, IAccountService accountService)
        {
            _service = service;
            _accountService = accountService;
        }

        [AllowAnonymous]
        [HttpGet]
        public ActionResult<IEnumerable<SongResponse>> GetAll()
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

        [AllowAnonymous]
        [HttpGet("song-ids")]
        public ActionResult<IEnumerable<string>> GetAllIds()
        {
            try
            {
                var response = _service.GetAllIds();
                return Ok(response);
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, new { message = ex.Message });
            }
        }

        [AllowAnonymous]
        [HttpGet("{id}")]
        public ActionResult<SongResponse> Get(string id)
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
        

        [HttpPost("create-song")]
        public ActionResult<SongResponse> Create(CreateSongRequest request)
        {
            try
            {
                //todo test if it works and add new role for user uploader?
                if (Account.Role != Role.Admin)
                    return Unauthorized(new { message = "Unauthorized" });
                var response = _service.Create(request, Account);
                return Ok(response);
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpDelete]
        public ActionResult Delete(DeleteSongRequest request)
        {
            try
            {
                var song = _service.Get(request.Id);

                //todo check if createdby is properly fetched ie not always null
                if (song.CreatedBy.Id != Account.Id.ToString() || Account.Role != Role.Admin)
                    return Unauthorized(new { message = "Unauthorized" });

                _service.Delete(request.Id);
                return Ok(new { message = "Song deleted successfully" });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpPost("flip-like")]
        public ActionResult FlipLike(FlipLikeRequest request)
        {
            try
            {
                var response = _service.FlipLike(request.Id, Account);
                return Ok(response);
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }
    }
}
