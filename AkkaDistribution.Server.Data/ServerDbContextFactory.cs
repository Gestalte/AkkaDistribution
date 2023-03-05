namespace AkkaDistribution.Server.Data
{
    public class ServerDbContextFactory : IServerDbContextFactory
    {
        public ServerDbContext Create()
        {
            return new ServerDbContext();
        }
    }
}
