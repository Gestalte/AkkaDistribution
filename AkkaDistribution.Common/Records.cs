public sealed record Manifest(DateTime Timesstamp, HashSet<ManifestFile> Files);
public sealed record ManifestFile(string Filename, string FileHash);
public sealed record FilePieceDelivery(string Filename, string FileHash, string FilePiece, int TotalPieces, int Position);
public sealed record MissingPieces(string Filename, string FileHash, List<string[]> MissingPositions);