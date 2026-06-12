using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using SongAppApi.Entities;
using SongAppApi.Helpers;
using File = SongAppApi.Entities.File;

namespace SongAppApi.Tests.Common
{
    public static class TestDbContextFactory
    {
        public static DataContext Create()
        {
            var config = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["ConnectionStrings:SongAppApiDatabase"] = "InMemory"
                })
                .Build();

            return new TestDataContext(config, Guid.NewGuid().ToString());
        }
    }

    public class TestDataContext : DataContext
    {
        private readonly string _dbName;

        public TestDataContext(IConfiguration configuration, string dbName)
            : base(configuration)
        {
            _dbName = dbName;
        }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            if (!optionsBuilder.IsConfigured)
            {
                optionsBuilder.UseInMemoryDatabase(_dbName);
            }
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {

            modelBuilder.Entity<Account>(b =>
            {
                b.HasKey(a => a.Id);
                b.OwnsMany(a => a.RefreshTokens);
            });

            modelBuilder.Entity<File>(b => b.HasKey(f => f.Id));

            modelBuilder.Entity<Song>(b =>
            {
                b.HasKey(s => s.Id);
                b.Ignore(s => s.PcaFeatures);
                b.HasOne(s => s.CreatedBy)
                    .WithMany(a => a.CreatedSongs)
                    .HasForeignKey(s => s.CreatedById);
                b.HasMany(s => s.LikedByAccounts)
                    .WithMany(a => a.LikedSongs);
            });

            modelBuilder.Entity<Playlist>(b =>
            {
                b.HasKey(p => p.Id);
                b.HasOne(p => p.CreatedBy)
                    .WithMany()
                    .HasForeignKey(p => p.CreatedById);
                b.HasMany(p => p.SavedByAccounts)
                    .WithMany(a => a.SavedPlaylists);
                b.HasMany(p => p.Songs)
                    .WithMany(s => s.SavedInPlaylists);
            });

            modelBuilder.Entity<Friendship>(b =>
            {
                b.HasKey(f => f.Id);
                b.HasOne(f => f.Sender).WithMany().HasForeignKey(f => f.SenderId);
                b.HasOne(f => f.Receiver).WithMany().HasForeignKey(f => f.ReceiverId);
            });

            modelBuilder.Entity<PlaylistInvitation>(b =>
            {
                b.HasKey(i => i.Id);
                b.HasOne(i => i.Sender).WithMany().HasForeignKey(i => i.SenderId);
                b.HasOne(i => i.Receiver).WithMany().HasForeignKey(i => i.ReceiverId);
                b.HasOne(i => i.Playlist).WithMany().HasForeignKey(i => i.PlaylistId);
            });
        }
    }
}
