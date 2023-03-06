using AkkaDistribution.Common;

namespace AkkaDistribution.Server.Data
{
    public interface IFilePartDeliveryRepository
    {
        void DeleteAllFilePartDeliveries();
        void OverwriteFilePartDeliveries(List<FilePartMessage> filePartMessages);
    }
}