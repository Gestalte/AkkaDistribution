using AkkaDistribution.Common;

namespace AkkaDistribution.Client.Data
{
    public interface IFilePartRepository
    {
        int Add(FilePartMessage filePartMessage);
        string GetFilePiecesByFilenameAndHash(string filename, string fileHash);
        Common.MissingPieces GetMissingFilePieces(Common.Manifest manifest);
    }
}