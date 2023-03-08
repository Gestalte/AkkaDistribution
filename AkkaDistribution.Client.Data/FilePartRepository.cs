using AkkaDistribution.Common;

namespace AkkaDistribution.Client.Data
{
    internal class FilePartRepository : IFilePartRepository
    {
        private readonly IClientDbContextFactory factory;

        public FilePartRepository(IClientDbContextFactory factory)
        {
            this.factory = factory;
        }

        private T WithTransaction<T>(Func<T> func, ClientDbContext context)
        {
            using var transaction = context.Database.BeginTransaction();

            try
            {
                var x = func();

                transaction.Commit();

                return x;
            }
            catch (Exception)
            {
                transaction.Rollback();
                throw;
            }
        }

        public int Add(FilePartMessage filePartMessage)
        {
            using var context = this.factory.Create();

            FilePart newFilePart = new()
            {
                Filename = filePartMessage.Filename,
                FileHash = filePartMessage.FileHash,
                Position = filePartMessage.Position,
                TotalPieces = filePartMessage.TotalPieces,
                Payload = filePartMessage.FilePart,
            };

            var id = WithTransaction<int>(() =>
            {
                context.FileParts.Add(newFilePart);

                context.SaveChanges();

                return newFilePart.FilePartId;
            }, context);

            return id;
        }
    }
}
