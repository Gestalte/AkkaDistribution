namespace AkkaDistribution.Client.Data
{
    public interface IManifestRepository
    {
        Common.Manifest GetNewestManifest();
        int SaveManifest(Common.Manifest manifest);
    }
}