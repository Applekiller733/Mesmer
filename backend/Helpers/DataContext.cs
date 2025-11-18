namespace SongAppApi.Helpers
{
    using Microsoft.EntityFrameworkCore;
    using SongAppApi.Entities;
    public class DataContext : DbContext
    {
        public DbSet<Account> Accounts { get; set; }
        public DbSet<File> Files { get; set; }
        public DbSet<Song> Songs { get; set; }
        public DbSet<Playlist> Playlists { get; set; }

        private readonly IConfiguration Configuration;

        public DataContext(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            var connectionString = Configuration.GetConnectionString("SongAppApiDatabase");
            optionsBuilder.UseMySql(connectionString,
                ServerVersion.AutoDetect(connectionString));
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            //todo check if cascade delete should be restricted?

            //.OnDelete(DeleteBehavior.Restrict); // prevents cascade delete

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
        }
    }
}
