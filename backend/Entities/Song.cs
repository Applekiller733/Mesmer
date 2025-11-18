using System.ComponentModel.DataAnnotations;

namespace SongAppApi.Entities
{
    public class Song
    {
        [Key]
        public int Id { get; set; }
        public string Name { get; set; }
        public string Artist { get; set; }
        public int Upvotes { get; set; }
        public DateTime CreatedAt { get; set; }
        public string? ImageUrl { get; set; }
        public File? Image { get; set; }
        public string? VideoUrl { get; set; }
        public File? Video { get; set; }
        public string? SoundUrl { get; set; }
        public File? Sound { get; set; }
        public int CreatedById { get; set; }
        public Account CreatedBy { get; set; }
        public List<Playlist> SavedInPlaylists { get; set; } = new List<Playlist>();
        public List<Account> LikedByAccounts { get; set; } = new List<Account>();
        //maybe also add image as a locally saved file?
        //public File? Image { get; set; }
    }
}
