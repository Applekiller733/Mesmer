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
                //todo move filestream to service and return reference to filestream here
                var file = _fileService.GetFileById(id);

                if (file == null || !System.IO.File.Exists(file.FilePath))
                    return NotFound();

                var stream = new FileStream(file.FilePath, FileMode.Open, FileAccess.Read);
                var provider = new FileExtensionContentTypeProvider();
                if (!provider.TryGetContentType(file.FileName, out var contentType))
                    contentType = "application/octet-stream";
                return File(stream, contentType, file.FileName);
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError);
            }
        }
    }
}
