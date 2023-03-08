using AkkaDistribution.Common;

namespace AkkaDistribution.Client.Data
{
    public interface IFilePartRepository
    {
        int Add(FilePartMessage filePartMessage);
    }
}