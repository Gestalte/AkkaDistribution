using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace AkkaDistribution.Client.Data
{

    public class ManifestRepository: IManifestRepository
    {
        private readonly IClientDbContextFactory factory;

        public ManifestRepository(IClientDbContextFactory factory)
        {
            this.factory = factory;
        }

        public int SaveManifest(Common.Manifest manifest)
        {
            using var context = this.factory.Create();
            using var transaction = context.Database.BeginTransaction();

            Manifest newManifest = new();

            try
            {
                newManifest = new()
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

                transaction.Commit();
            }
            catch (Exception)
            {
                transaction.Rollback();
                throw;
            }

            return newManifest.ManifestId;
        }

        public Common.Manifest GetNewestManifest()
        {
            using var context = this.factory.Create();

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
