using AkkaDistribution.Common;

namespace AkkaDistribution.Client.Data
{
    internal interface IFilePartRepository
    {
        int Add(FilePartMessage filePartMessage);
    }
}