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
            var response = _service.GetAll();
            return Ok(response);
        }

        [AllowAnonymous]
        [HttpGet("song-ids")]
        public ActionResult<IEnumerable<int>> GetAllIds()
        {
            var response = _service.GetAllIds();
            return Ok(response);
        }

        [AllowAnonymous]
        [HttpGet("{id:int}")]
        public ActionResult<SongResponse> Get(int id)
        {
            var response = _service.Get(id);
            return Ok(response);
        }

        [HttpPost("create-song")]
        public ActionResult<SongResponse> Create(CreateSongRequest request)
        {
            //todo test if it works and add new role for user uploader?
            if (Account.Role != Role.Admin)
                return Unauthorized(new {message = "Unauthorized"});
            var response = _service.Create(request, Account);
            return Ok(response);
        }

        [HttpDelete]
        public ActionResult Delete(DeleteSongRequest request)
        {
            var song = _service.Get(request.Id);

            //todo check if createdby is properly fetched ie not always null
            if (song.CreatedBy.Id != Account.Id || Account.Role != Role.Admin)
                return Unauthorized(new { message = "Unauthorized" });

            _service.Delete(request.Id);
            return Ok(new { message = "Song deleted successfully" });
        }

        [HttpPost("flip-like")]
        public ActionResult FlipLike(FlipLikeRequest request)
        {
            var response = _service.FlipLike(request.Id, Account);
            return Ok(response);
        }
    }
}
