namespace AkkaDistribution.Client.Data
{
    public interface IClientDbContextFactory
    {
        ClientDbContext Create();
    }
}