using AkkaDistribution.Common;
using Microsoft.EntityFrameworkCore;

namespace AkkaDistribution.Server.Data
{
    public class FilePartDeliveryRepository : IFilePartDeliveryRepository
    {
        private readonly IServerDbContextFactory factory;

        public FilePartDeliveryRepository(IServerDbContextFactory factory)
        {
            this.factory = factory;
        }

        private T WithTransaction<T>(Func<T> func, ServerDbContext context)
        {
            using var transaction = context.Database.BeginTransaction();

            try
            {
                return func();
            }
            catch (Exception)
            {
                transaction.Rollback();
                throw;
            }
        }

        public void DeleteAllFilePartDeliveries()
        {
            using var context = this.factory.Create();
            using var transaction = context.Database.BeginTransaction();

            try
            {
                var all = context.FilePartDeliveries.ToList();
                context.FilePartDeliveries.RemoveRange(all);

                context.SaveChanges();

                transaction.Commit();
            }
            catch (Exception)
            {
                transaction.Rollback();
                throw;
            }
        }

        public void OverwriteFilePartDeliveries(List<FilePartMessage> filePartMessages)
        {
            using var context = this.factory.Create();
            using var transaction = context.Database.BeginTransaction();

            try
            {
                context.FilePartDeliveries.AddRange(filePartMessages.Select(s => new FilePartDelivery
                {
                    Position = s.Position,
                    TotalPieces = s.TotalPieces,
                    Filename = s.Filename,
                    FileHash = s.FileHash,
                    Payload = s.FilePart,
                }));

                context.SaveChanges();

                transaction.Commit();
            }
            catch (Exception)
            {
                transaction.Rollback();

                throw;
            }
        }

        public List<FilePartMessage> GetFilePartsDeliveriesByManifest(Common.Manifest manifest)
        {
            using var context = this.factory.Create();
            using var transaction = context.Database.BeginTransaction();

            var filePartDeliveries = context.FilePartDeliveries
                .AsNoTracking()
                .Where(w => manifest.Files.Select(s => s.Filename)
                .Contains(w.Filename))
                .ToList();

            return filePartDeliveries
                .Select(s => new Common.FilePartMessage(s.Filename, s.FileHash, s.TotalPieces, s.Payload, s.Position))
                .ToList();
        }
    }
}
