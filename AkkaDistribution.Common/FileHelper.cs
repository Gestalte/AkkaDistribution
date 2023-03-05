using System.Diagnostics;
using System.Security.Cryptography;

namespace AkkaDistribution.Common
{
    public class FileHelper
    {
        public static string GetSHA1Hashes(string filepath)
        {
            using SHA1 sha1 = SHA1.Create();
            using var stream = File.OpenRead(filepath);
            byte[] hashBytes = sha1.ComputeHash(stream);

            return BitConverter.ToString(hashBytes).Replace("-", "");
        }

        public static Manifest GenerateManifestFromDirectory(FileBox filelocation)
        {
            var filepaths = Directory.EnumerateFiles
                (filelocation.DirectoryPath
                , "*.*"
                , SearchOption.AllDirectories
                )
                .ToArray();

            var manifestFiles = filepaths
                .Select(s => new ManifestFile(s[(filelocation.DirectoryPath.Length+1)..], GetSHA1Hashes(s)));

            return new Manifest(DateTime.UtcNow, manifestFiles.ToHashSet());
        }

        public static Manifest Difference(Manifest oldManifest, Manifest newManifest)
        {
            var fileDiff = newManifest.Files
                .Except(oldManifest.Files)
                .ToHashSet();

            return new Manifest(DateTime.UtcNow, fileDiff);
        }

        private const int batchSize = 125;

        public static List<FilePartMessage> SplitIntoMessages(string pathToSend, string filename, string fileHash)
        {
            var bytes = File.ReadAllBytes(pathToSend);

            var chunks = Convert.ToBase64String(bytes)
                .Chunk(batchSize)
                .ToArray();

            FilePartMessage[] filePartMessages = new FilePartMessage[chunks.Length];

            for (int i = 0; i < chunks.Length; i++)
            {
                filePartMessages[i] = new FilePartMessage
                    (filename
                    , fileHash
                    , chunks.Length
                    , chunks[i].ToString()!
                    , i
                    );
            }

            return filePartMessages.ToList();
        }
    }
}
