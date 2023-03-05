namespace AkkaDistribution.Server.Data
{
    public class FilePartDelivery
    {
        public int FilePartDeliveryId { get; set; }
        public int Position { get; set; }
        public int TotalPieces { get; set; }
        public string Filename { get; set; }
        public string FileHash { get; set; }
        public string Payload { get; set; }
    }
}
