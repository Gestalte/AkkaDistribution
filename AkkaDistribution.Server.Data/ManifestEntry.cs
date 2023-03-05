namespace AkkaDistribution.Server.Data
{
    public class ManifestEntry
    {
        public int ManifestEntryId { get; set; }
        public string Filename { get; set; }
        public string FileHash { get; set; }
        public string FilePiece { get; set; }
        public int ManifestId { get; set; }
    }
}
