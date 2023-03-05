using Microsoft.EntityFrameworkCore;

namespace AkkaDistribution.Server.Data
{
    public class ServerDbContext : DbContext
    {
        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            optionsBuilder.UseSqlite("Data Source=filesync.sqlite");
        }

        public DbSet<Manifest> Manifests { get; set; }
        public DbSet<ManifestEntry> ManifestEntries { get; set; }
        public DbSet<FilePartDelivery> FilePartDeliveries { get; set; }
    }
}
