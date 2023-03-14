namespace AkkaDistribution.Common
{
    public static class Extensions
    {
        public static Manifest Compare(this Manifest oldManifest,Manifest newManifest)
        {
            var diff = oldManifest.Files.Except(newManifest.Files).ToHashSet();

            return new Manifest(DateTime.UtcNow, diff);
        }
    }
}
