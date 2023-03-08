namespace AkkaDistribution.Client.Data
{
    public class ClientDbContextFactory : IClientDbContextFactory
    {
        public ClientDbContext Create()
        {
            return new ClientDbContext();
        }
    }
}
