using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.StaticFiles;
using SongAppApi.Authorization;
using SongAppApi.Entities;
using SongAppApi.Helpers.Enumerators;
using SongAppApi.Models.Songs;
using SongAppApi.Services;

namespace SongAppApi.Controllers
{
    [Authorization.Authorize]
    [ApiController]
    [Route("[controller]")]
    public class SongsController : BaseController
    {
        private readonly ISongService _service;
        private readonly IAccountService _accountService;
        private readonly IFileService _fileService;

        public SongsController(ISongService service, IAccountService accountService,
            IFileService fileService)
        {
            _service = service;
            _accountService = accountService;
            _fileService = fileService;
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
                return Ok(_service.GetAllIds());
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

        [AllowAnonymous]
        [HttpGet("{id}/audio", Name = nameof(GetAudio))]
        public IActionResult GetAudio(string id)
        {
            try
            {
                var file = _service.GetSoundFile(id);
                if (file == null)
                    return NotFound();

                // Redirect to a presigned S3 URL. S3 serves range requests
                // natively, so audio scrubbing keeps working against S3 directly.
                var url = _fileService.GetPresignedDownloadUrl(file);
                return Redirect(url);
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, new { message = ex.Message });
            }
        }

        [HttpPost("create-song")]
        public ActionResult<SongResponse> Create([FromForm] CreateSongRequest request)
        {
            try
            {
                if (Account == null) return Unauthorized();

                var response = _service.Create(request, Account);
                return Ok(response);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, new { message = ex.Message });
            }
        }

        [HttpPost("flip-like")]
        public ActionResult FlipLike(FlipLikeRequest request)
        {
            try
            {
                if (Account == null) return Unauthorized();
                var response = _service.FlipLike(request.Id, Account);
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
                if (Account == null) return Unauthorized();

                var song = _service.Get(request.Id);
                var isCreator = song.CreatedBy.Id == Account.Id.ToString();
                var isAdmin = Account.Role == Role.Admin;
                if (!isCreator && !isAdmin)
                    return Unauthorized(new { message = "Unauthorized" });

                _service.Delete(request.Id);
                return Ok(new { message = "Song deleted successfully" });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [Authorization.Authorize(Role.Admin)]
        [HttpPut]
        public ActionResult<SongResponse> Update(UpdateSongRequest request)
        {
            try
            {
                if (Account == null) return Unauthorized();
                var response = _service.Update(request);
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