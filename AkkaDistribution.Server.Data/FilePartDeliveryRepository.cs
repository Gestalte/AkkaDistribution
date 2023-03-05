using AkkaDistribution.Common;

namespace AkkaDistribution.Server.Data
{
    public class FilePartDeliveryRepository : IFilePartDeliveryRepository
    {
        private readonly IServerDbContextFactory factory;

        public FilePartDeliveryRepository(IServerDbContextFactory factory)
        {
            this.factory = factory;
        }

        public void OverwriteFilePartDeliveries(List<FilePartMessage> filePartMessages)
        {
            using var context = this.factory.Create();
            using var transaction = context.Database.BeginTransaction();

            try
            {
                context.FilePartDeliveries.ToList().Clear();

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
    }
}
