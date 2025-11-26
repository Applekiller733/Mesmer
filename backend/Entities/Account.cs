using System.ComponentModel.DataAnnotations;
using System.Data;
using MassTransit;

namespace SongAppApi.Entities
{
    public class Account
    {
        [Key]
        public Guid Id { get; set; } = NewId.NextSequentialGuid();
        public string UserName { get; set; }
        public string Email { get; set; }
        public string PasswordHash { get; set; }
        public bool AcceptTerms { get; set; }
        public Role Role { get; set; }
        public string? VerificationToken { get; set; }
        public DateTime? Verified { get; set; }
        public bool IsVerified => Verified.HasValue || PasswordReset.HasValue;
        public string? ResetToken { get; set; }
        public DateTime? ResetTokenExpires { get; set; }
        public DateTime? PasswordReset { get; set; }
        public DateTime Created { get; set; }
        public DateTime? Updated { get; set; }
        public File? ProfilePicture { get; set; }
        public List<Song> CreatedSongs { get; set; }
        public List<Song> LikedSongs { get; set; }
        public List<Playlist> SavedPlaylists { get; set; }
        public List<RefreshToken> RefreshTokens { get; set; }

        public bool OwnsToken(string token)
        {
            return this.RefreshTokens?.Find(x => x.Token == token) != null;
        }
    }
}
