namespace AkkaDistribution.Server.Data
{
    public interface IServerDbContextFactory
    {
        ServerDbContext Create();
    }
}