using SongAppApi.Entities;
using System.ComponentModel.DataAnnotations;
using SongAppApi.Models.Accounts;
using SongAppApi.Models.Songs;
using SongAppApi.Helpers.Enumerators;

namespace SongAppApi.Models.Playlist
{
    public class PlaylistResponse
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
        public PlaylistVisibility Visibility { get; set; }
        public AccountResponse CreatedBy { get; set; }
        //public int CreatedById { get; set; }
        public List<SongResponse> Songs { get; set; }
        //todo maybe actually include?
        //public List<AccountResponse> SavedByAccounts { get; set; }
    }
}
