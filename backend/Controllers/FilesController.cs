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
