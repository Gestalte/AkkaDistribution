using AkkaDistribution.Common;

namespace AkkaDistribution.Server.Data
{
    public interface IFilePartDeliveryRepository
    {
        void OverwriteFilePartDeliveries(List<FilePartMessage> filePartMessages);
    }
}