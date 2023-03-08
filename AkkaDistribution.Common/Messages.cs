namespace AkkaDistribution.Common
{
    public sealed record Manifest(DateTime Timestamp, HashSet<ManifestFile> Files);
    public sealed record ManifestFile(string Filename, string FileHash);
    public sealed record MissingPieces(string Filename, string FileHash, List<string[]> MissingPositions);
    public sealed record ManifestRequest();
    public sealed record ManifestUpdate(Manifest Manifest);
    public sealed record FilePartMessage(string Filename, string FileHash, int TotalPieces, string FilePart, int Position);
    public sealed record ManifestBeingCreated(bool IsDone);
}