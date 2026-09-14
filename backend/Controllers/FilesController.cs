using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.StaticFiles;
using SongAppApi.Models.Files;
using SongAppApi.Services;

namespace SongAppApi.Controllers
{
    //todo add [Authorization.Authorize]
    [ApiController]
    [Route("[controller]")]
    public class FilesController : BaseController
    {
        private readonly IFileService _fileService;
        //private readonly IAccountService _accountService;

        public FilesController(IFileService fileService)
        {
            _fileService = fileService;
        }
        [HttpPost]
        public ActionResult<string> Post([FromForm] FileModel file)
        {
            try
            {
                var response = _fileService.Create(file);
                return Ok(response);
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError);
            }
        }

        // Step 1 of a direct-to-S3 upload: hand the client a presigned PUT URL.
        // The client then PUTs the file bytes straight to S3 (bypassing the API
        // Gateway payload limit), and calls confirm-upload afterwards.
        [HttpPost("presign-upload")]
        public ActionResult<PresignedUpload> PresignUpload([FromBody] PresignUploadRequest request)
        {
            try
            {
                var result = _fileService.PresignUpload(
                    request.Category, request.FileName, request.KeyPrefix);
                return Ok(result);
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
            catch (Exception)
            {
                return StatusCode(StatusCodes.Status500InternalServerError);
            }
        }

        // Step 2: the client reports the upload finished; verify + persist the
        // File row and return its id (which songs/accounts can then reference).
        [HttpPost("confirm-upload")]
        public ActionResult<string> ConfirmUpload([FromBody] ConfirmUploadRequest request)
        {
            try
            {
                var file = _fileService.ConfirmUpload(
                    request.Key, request.FileName, request.Category);
                return Ok(file.Id.ToString());
            }
            catch (KeyNotFoundException)
            {
                return NotFound();
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
            catch (Exception)
            {
                return StatusCode(StatusCodes.Status500InternalServerError);
            }
        }

        [HttpGet("{id}")]
        public ActionResult Get(string id)
        {
            try
            {
                var file = _fileService.GetFileById(id);
                if (file == null)
                    return NotFound();

                // Redirect the client to a short-lived presigned S3 URL so the
                // bytes transfer straight from S3 (media elements and range
                // requests follow the redirect transparently).
                var url = _fileService.GetPresignedDownloadUrl(file);
                return Redirect(url);
            }
            catch (KeyNotFoundException)
            {
                return NotFound();
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError);
            }
        }
    }
}
