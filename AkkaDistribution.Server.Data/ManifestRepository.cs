using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace AkkaDistribution.Server.Data
{
    public class ManifestRepository: IManifestRepository
    {
        private readonly IServiceScopeFactory scopeFactory;

        public ManifestRepository(IServiceScopeFactory scopeFactory)
        {
            this.scopeFactory = scopeFactory;
        }

        public int SaveManifest(Common.Manifest manifest)
        {
            using var scope = this.scopeFactory.CreateScope();

            var context = scope.ServiceProvider.GetRequiredService<ServerDbContext>();

            Manifest newManifest = new()
            {
                Timestamp = manifest.Timestamp,
                ManifestEntries = manifest.Files.Select(s => new ManifestEntry
                {
                    Filename = s.Filename,
                    FileHash = s.FileHash,
                }).ToList()
            };

            context.Manifests.Add(newManifest);

            context.SaveChanges();

            return newManifest.ManifestId;
        }

        public Common.Manifest GetNewestManifest()
        {
            using var scope = this.scopeFactory.CreateScope();

            var context = scope.ServiceProvider.GetRequiredService<ServerDbContext>();

            var manifest = context.Manifests
                .AsNoTracking()
                .Include(i => i.ManifestEntries)
                .OrderByDescending(o => o.ManifestId)
                .FirstOrDefault();

            if (manifest == null)
            {
                return new Common.Manifest(DateTime.UtcNow, new());
            }

            var commonManifestFiles = manifest.ManifestEntries
                .Select(s => new Common.ManifestFile(s.Filename, s.FileHash))
                .ToHashSet();

            return new Common.Manifest(DateTime.UtcNow, commonManifestFiles);
        }
    }
}
