namespace AkkaDistribution.Client.Data
{
    public class ManifestEntry
    {
        public int ManifestEntryId { get; set; }
        public string Filename { get; set; }
        public string FileHash { get; set; }
        public int ManifestId { get; set; }
    }
}
