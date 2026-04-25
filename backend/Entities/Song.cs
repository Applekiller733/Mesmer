using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using MassTransit;
using Pgvector;

namespace SongAppApi.Entities
{
    public class Song
    {
        [Key] 
        public Guid Id { get; set; } = NewId.NextSequentialGuid();
        [Required]
        public string Name { get; set; }
        [Required]
        public string Artist { get; set; }
        public int Upvotes { get; set; }
        public DateTime CreatedAt { get; set; }

        //image
        public string? ImageUrl { get; set; }
        public File? Image { get; set; }
        public Guid? ImageId { get; set; }

        //video
        public string? VideoUrl { get; set; }
        public File? Video { get; set; }
        public Guid? VideoId { get; set; }

        //sound
        public string? SoundUrl { get; set; }
        public File? Sound { get; set; }
        public Guid? SoundId { get; set; }

        //references
        public Guid CreatedById { get; set; }
        public Account CreatedBy { get; set; }
        public List<Playlist> SavedInPlaylists { get; set; } = new List<Playlist>();
        public List<Account> LikedByAccounts { get; set; } = new List<Account>();


        //Song Metadata
        public string? MBId { get; set; }
        public float? Tempo { get; set; }
        public float? Danceability { get; set; }
        public float? Energy { get; set; }
        public float? Valence { get; set; } // "Mood" (0 = sad, 1 = happy)
        public Vector? Embedding { get; set; }
    }
}
