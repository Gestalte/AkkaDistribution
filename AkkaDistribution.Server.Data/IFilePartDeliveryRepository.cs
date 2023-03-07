using AkkaDistribution.Common;

namespace AkkaDistribution.Server.Data
{
    public interface IFilePartDeliveryRepository
    {
        void DeleteAllFilePartDeliveries();
        List<FilePartMessage> GetFilePartsDeliveriesByManifest(Common.Manifest manifest);
        void OverwriteFilePartDeliveries(List<FilePartMessage> filePartMessages);
    }
}