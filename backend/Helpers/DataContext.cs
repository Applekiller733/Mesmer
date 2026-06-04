namespace SongAppApi.Helpers
{
    using Microsoft.EntityFrameworkCore;
    using Pgvector;
    using SongAppApi.Entities;
    public class DataContext : DbContext
    {
        public DbSet<Account> Accounts { get; set; }
        public DbSet<File> Files { get; set; }
        public DbSet<Song> Songs { get; set; }
        public DbSet<Playlist> Playlists { get; set; }
        public DbSet<Friendship> Friendships { get; set;}
        public DbSet<PlaylistInvitation> PlaylistInvitations { get; set; }

        private readonly IConfiguration Configuration;

        public DataContext(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            //optionsBuilder.UseMySql(connectionString,
            //    ServerVersion.AutoDetect(connectionString));
            var connectionString = Configuration.GetConnectionString("SongAppApiDatabase");
            optionsBuilder.UseNpgsql(
                connectionString,
                o => o.UseVector()
            );
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            //extension for pgvector
            modelBuilder.HasPostgresExtension("vector");

            //todo check if cascade delete should be restricted?

            //.OnDelete(DeleteBehavior.Restrict); // prevents cascade delete

            //prevents db from auto generating ids, instead we programatically gen the guids
            modelBuilder.Entity<Account>()
                .Property(x => x.Id)
                .ValueGeneratedNever();

            modelBuilder.Entity<File>()
                .Property(x => x.Id)
                .ValueGeneratedNever();

            modelBuilder.Entity<Song>()
                .Property(x => x.Id)
                .ValueGeneratedNever();

            modelBuilder.Entity<Song>()
                .Property(s => s.PcaFeatures)
                .HasColumnType("vector(40)");

            modelBuilder.Entity<Playlist>()
                .Property(x => x.Id)
                .ValueGeneratedNever();

            //modelBuilder.Entity<RefreshToken>()
            //    .Property(x => x.Id)
            //    .ValueGeneratedNever();

            //for db relations
            modelBuilder.Entity<Song>()
                .HasOne(s => s.CreatedBy)
                .WithMany(a => a.CreatedSongs)
                .HasForeignKey(s => s.CreatedById);

            //.OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Song>()
                .HasMany(s => s.LikedByAccounts)
                .WithMany(a => a.LikedSongs)
                .UsingEntity(j => j.ToTable("AccountSongLikes"));

            //one to many pt createdby
            modelBuilder.Entity<Playlist>()
                .HasOne(p => p.CreatedBy)
                .WithMany()
                .HasForeignKey(p => p.CreatedById);
            //.OnDelete(DeleteBehavior.Restrict);

            //.OnDelete(DeleteBehavior.Restrict);

            //many to many pt savedby
            modelBuilder.Entity<Playlist>()
                .HasMany(p => p.SavedByAccounts)
                .WithMany(a => a.SavedPlaylists)
                .UsingEntity(j => j.ToTable("accountplaylist"));


            //friendships 

            modelBuilder.Entity<Friendship>(entity =>
            {
                // Two navigation properties (Sender, Receiver) both pointing at
                // Account. EF Core can't infer which is which without explicit
                // configuration, so we declare both relationships manually.
                //
                // OnDelete: Restrict prevents EF from auto-cascading Account
                // deletions. If you delete a user account, you'd want to clean up
                // their friendships explicitly in the deletion service rather
                // than rely on cascade — gives you control over notifications,
                // soft delete, etc.

                entity.HasOne(f => f.Sender)
                    .WithMany()  // No back-navigation collection on Account; we
                                 // can add it later if needed (e.g. account.SentFriendRequests)
                    .HasForeignKey(f => f.SenderId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(f => f.Receiver)
                    .WithMany()
                    .HasForeignKey(f => f.ReceiverId)
                    .OnDelete(DeleteBehavior.Restrict);

                // Unique constraint: at most one row per directional pair. This is
                // also our primary index for "does a friendship row exist between
                // these two users in this direction" lookups.
                entity.HasIndex(f => new { f.SenderId, f.ReceiverId })
                    .IsUnique();

                // Reverse-direction index. The most common query — "are A and B
                // friends?" — needs to check both (A, B) and (B, A). Without this
                // index, the (B, A) lookup falls back to a scan.
                entity.HasIndex(f => new { f.ReceiverId, f.SenderId });

                // Status index: speeds up "show me incoming Pending requests" and
                // similar filters on a single user's edges. Composite with
                // ReceiverId because that's the typical use ("MY incoming pending").
                entity.HasIndex(f => new { f.ReceiverId, f.Status });
            });

            modelBuilder.Entity<PlaylistInvitation>(entity =>
            {
                entity.Property(p => p.Id).ValueGeneratedNever();
                entity.HasOne(p => p.Playlist)
                    .WithMany()
                    .HasForeignKey(p => p.PlaylistId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(p => p.Sender)
                    .WithMany()
                    .HasForeignKey(p => p.SenderId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(p => p.Receiver)
                    .WithMany()
                    .HasForeignKey(p => p.ReceiverId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasIndex(p => new { p.PlaylistId, p.ReceiverId })
                    .IsUnique();

                entity.HasIndex(p => new { p.ReceiverId, p.CreatedAt });

                entity.HasIndex(p => new { p.SenderId, p.CreatedAt });
            });


        }
    }
}
