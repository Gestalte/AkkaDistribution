using System;

namespace MyApp // Note: actual namespace depends on the project name.
{
    internal class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("Hello World!");
        }

        public static string[] GetSHA1Hashes(string[] filepaths)
        {
            if (filepaths == null || filepaths.Length == 0)
            {
                return Array.Empty<string>();
            }

            var output = new string[filepaths.Length];

            for (int i = 0; i < filepaths.Length; i++)
            {
                using SHA1 sha1 = SHA1.Create();
                using var stream = File.OpenRead(filepaths[i]);
                byte[] hashBytes = sha1.ComputeHash(stream);
                output[i] = BitConverter.ToString(hashBytes).Replace("-", "");
            }

            return output;
        }
    }
}