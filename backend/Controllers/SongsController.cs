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
                // Post-process so SoundUrl points to the streaming endpoint when
                // the song has an uploaded file. (See Get/Create for the pattern.)
                foreach (var s in response)
                {
                    PopulateSoundUrlIfUploaded(s);
                }
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
                PopulateSoundUrlIfUploaded(response);
                return Ok(response);
            }
            catch (Exception ex)
            {
                return NotFound(new { message = ex.Message });
            }
        }

        // streams the audio file with HTTP range support so <audio> can seek.
        // anonymous so the player can fetch without managing auth headers in the
        // <audio> tag. Tighten this if you need access control.
        [AllowAnonymous]
        [HttpGet("{id}/audio")]
        public IActionResult GetAudio(string id)
        {
            try
            {
                var file = _service.GetSoundFile(id);
                if (file == null || !System.IO.File.Exists(file.FilePath))
                    return NotFound();

                var stream = new FileStream(
                    file.FilePath,
                    FileMode.Open,
                    FileAccess.Read,
                    FileShare.Read);

                var provider = new FileExtensionContentTypeProvider();
                if (!provider.TryGetContentType(file.FileName, out var contentType))
                    contentType = "application/octet-stream";

                // enableRangeProcessing: true is the critical bit — it lets the
                // browser do byte-range requests for seeking.
                return File(stream, contentType, enableRangeProcessing: true);
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, new { message = ex.Message });
            }
        }

        // now accepts multipart form data, the CreateSongRequest model
        // contains the optional IFormFile SoundFile alongside the metadata.
        [HttpPost("create-song")]
        public ActionResult<SongResponse> Create([FromForm] CreateSongRequest request)
        {
            try
            {
                if (Account == null) return Unauthorized();

                var response = _service.Create(request, Account);
                PopulateSoundUrlIfUploaded(response);
                return Ok(response);
            }
            catch (InvalidOperationException ex)
            {
                // Validation failures from FileService (size, extension)
                return BadRequest(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, new { message = ex.Message });
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


        // Helper: when a song has an uploaded audio file, set SoundUrl to the
        // streaming endpoint so the frontend can plug it straight into <audio src=…>.
        // For songs that only have an external URL, leave it as-is.
        private void PopulateSoundUrlIfUploaded(SongResponse s)
        {
            if (s == null || string.IsNullOrEmpty(s.Id)) return;

            var file = _service.GetSoundFile(s.Id);
            if (file == null) return;

            var absolute = Url.Action(
                action: nameof(GetAudio),
                controller: "Songs",
                values: new { id = s.Id },
                protocol: Request.Scheme,
                host: Request.Host.Value);

            if (!string.IsNullOrEmpty(absolute))
                s.SoundUrl = absolute;
        }

    }
}