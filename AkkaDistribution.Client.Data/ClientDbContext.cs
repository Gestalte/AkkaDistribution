using Microsoft.EntityFrameworkCore;

namespace AkkaDistribution.Client.Data
{
    public class ClientDbContext : DbContext
    {
        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            optionsBuilder.UseSqlite("Data Source=filesync.sqlite;Pooling=false;Cache=Shared;");
        }

        public DbSet<FilePart> FileParts { get; set; }
        public DbSet<Manifest> Manifests { get; set; }
        public DbSet<ManifestEntry> ManifestEntries { get; set; }
    }
}
