using System.ComponentModel.DataAnnotations;
using MassTransit;

namespace SongAppApi.Entities
{
    public class PlaylistInvitation
    {
        [Key]
        public Guid Id { get; set; } = NewId.NextSequentialGuid();

        public Guid PlaylistId { get; set; }
        public Playlist Playlist { get; set; }
        public Guid SenderId { get; set; }
        public Account Sender { get; set; }
        public Guid ReceiverId { get; set; }
        public Account Receiver { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}