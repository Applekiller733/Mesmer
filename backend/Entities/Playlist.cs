using System.ComponentModel.DataAnnotations;
using MassTransit;
using SongAppApi.Helpers.Enumerators;

namespace SongAppApi.Entities
{
    public class Playlist
    {
        [Key]
        public Guid Id { get; set; } = NewId.NextSequentialGuid();
        public string Name { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
        public Guid CreatedById { get; set; }
        public Account CreatedBy { get; set; }
        public PlaylistVisibility Visibility { get; set; } = PlaylistVisibility.Private;
        public File? Image { get; set; }
        public List<Song> Songs { get; set; } = new List<Song>();
        public List<Account> SavedByAccounts { get; set; } = new List<Account>();
    }
}
