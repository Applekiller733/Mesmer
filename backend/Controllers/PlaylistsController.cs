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

        [HttpGet("{id:int}")]
        public ActionResult<PlaylistResponse> Get(int id)
        {
            var response = _service.Get(id);
            return Ok(response);
        }

        [HttpGet]
        public ActionResult<IEnumerable<PlaylistResponse>> GetAll()
        {
            var response = _service.GetAll();
            return Ok(response);
        }

        [HttpGet("made-by/{accountid:int}")]
        public ActionResult<IEnumerable<PlaylistResponse>> GetAllCreatedByAccount(int accountid)
        {
            var response = _service.GetCreatedByAccount(accountid);
            return Ok(response);
        }

        [HttpGet("saved-by/{accountid:int}")]
        public ActionResult<IEnumerable<PlaylistResponse>> GetAllSavedByAccount(int accountid)
        {
            var response = _service.GetSavedByAccount(accountid);
            return Ok(response);
        }

        [HttpPost("create-playlist")]
        public ActionResult<PlaylistResponse> CreatePlaylist(CreatePlaylistRequest request)
        {
            //todo test if it works
            var response = _service.Create(request, Account);
            return Ok(response);
        }

        //todo remove the id from the URL and instead add to request?
        [HttpPut]
        public ActionResult<PlaylistResponse> UpdatePlaylist(UpdatePlaylistRequest request)
        {
            var playlist = _service.Get(request.Id);
            if (playlist.CreatedBy.Id != Account.Id || Account.Role != Role.Admin)
                return Unauthorized(new { message = "Unauthorized" });

            var response = _service.Update(request.Id, request);
            return Ok(response);
        }

        [HttpDelete]
        public ActionResult Delete(DeletePlaylistRequest request)
        {
            var playlist = _service.Get(request.Id);
            if (playlist.CreatedBy.Id != Account.Id || Account.Role != Role.Admin)
                return Unauthorized(new { message = "Unauthorized" });
            
            _service.Delete(request.Id);
            return Ok(new { message = "Playlist successfully deleted" });
        }
    }
}
