using System.ComponentModel.DataAnnotations;

namespace SongAppApi.Entities
{
    public class Playlist
    {
        [Key]
        public int Id { get; set; }
        public string Name { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
        public int CreatedById { get; set; }
        public Account CreatedBy { get; set; }
        public File? Image { get; set; }
        public List<Song> Songs { get; set; } = new List<Song>();
        public List<Account> SavedByAccounts { get; set; } = new List<Account>();
    }
}
