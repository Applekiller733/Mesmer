using SongAppApi.Entities;
using SongAppApi.Helpers.Enumerators;

namespace SongAppApi.Tests.Common
{
    public static class EntityBuilder
    {
        public static Account NewAccount(
            Guid? id = null,
            string? userName = null,
            string? email = null,
            Role role = Role.User,
            bool verified = true,
            string? passwordHash = null,
            string? friendCode = null)
        {
            return new Account
            {
                Id = id ?? Guid.NewGuid(),
                UserName = userName ?? $"user-{Guid.NewGuid():N}",
                Email = email ?? $"{Guid.NewGuid():N}@example.com",
                PasswordHash = passwordHash ?? BCrypt.Net.BCrypt.HashPassword("Password!1"),
                Role = role,
                FriendCode = friendCode ?? Guid.NewGuid().ToString("N")[..8],
                Created = DateTime.UtcNow,
                Verified = verified ? DateTime.UtcNow : (DateTime?)null,
                RefreshTokens = new List<RefreshToken>(),
                AcceptTerms = true,
            };
        }

        public static Song NewSong(
            Account creator,
            Guid? id = null,
            string? name = null,
            string? artist = null)
        {
            return new Song
            {
                Id = id ?? Guid.NewGuid(),
                Name = name ?? "Test Song",
                Artist = artist ?? "Test Artist",
                CreatedBy = creator,
                CreatedById = creator.Id,
                CreatedAt = DateTime.UtcNow,
                Upvotes = 0,
            };
        }

        public static Playlist NewPlaylist(
            Account creator,
            Guid? id = null,
            string? name = null,
            PlaylistVisibility visibility = PlaylistVisibility.Private,
            List<Song>? songs = null)
        {
            var p = new Playlist
            {
                Id = id ?? Guid.NewGuid(),
                Name = name ?? "Test Playlist",
                CreatedBy = creator,
                CreatedById = creator.Id,
                Visibility = visibility,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
                Songs = songs ?? new List<Song>(),
                SavedByAccounts = new List<Account> { creator },
            };
            return p;
        }
    }
}
