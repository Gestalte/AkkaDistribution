namespace AkkaDistribution.Common
{
    public sealed class FileBox
    {
        public FileBox(string boxName)
        {
            DirectoryPath = Directory.CreateDirectory(Path.Combine(Directory.GetCurrentDirectory(), boxName)).FullName;
        }

        public string DirectoryPath { get; private set; }

        public List<string> GetFilesInBox()
            => Directory.GetFiles(DirectoryPath).ToList();

        public static string? FindFilePath(string filename, FileBox box)
        {
            return Directory.GetFiles(box.DirectoryPath)
                .Where(s => Path.GetFileName(s) == filename)
                .FirstOrDefault()
                ?? throw new ArgumentException("File not found in SendBox", nameof(filename));
        }
    }
}
