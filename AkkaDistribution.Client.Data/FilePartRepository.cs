using AkkaDistribution.Common;
using Microsoft.EntityFrameworkCore.ValueGeneration;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;

namespace AkkaDistribution.Client.Data
{
    public class FilePartRepository : IFilePartRepository
    {
        private readonly IClientDbContextFactory factory;

        public FilePartRepository(IClientDbContextFactory factory)
        {
            this.factory = factory;
        }

        private T WithTransaction<T>(Func<T> func, ClientDbContext context)
        {
            using var transaction = context.Database.BeginTransaction();

            try
            {
                var x = func();

                transaction.Commit();

                return x;
            }
            catch (Exception)
            {
                transaction.Rollback();
                throw;
            }
        }

        public int Add(FilePartMessage filePartMessage)
        {
            using var context = this.factory.Create();

            FilePart newFilePart = new()
            {
                Filename = filePartMessage.Filename,
                FileHash = filePartMessage.FileHash,
                Position = filePartMessage.Position,
                TotalPieces = filePartMessage.TotalPieces,
                Payload = filePartMessage.FilePart,
            };

            var id = WithTransaction<int>(() =>
            {
                context.FileParts.Add(newFilePart);

                context.SaveChanges();

                return newFilePart.FilePartId;
            }, context);

            return id;
        }

        public bool CheckThatDbMatchesManifest(Common.Manifest manifest)
        {
            using var context = this.factory.Create();

            foreach (var file in manifest.Files)
            {
                var t = context.FileParts
                    .Where(w => w.Filename == file.Filename)
                    .Where(w => w.FileHash == file.FileHash)
                    .Select(s => new Tuple<int, int>(s.TotalPieces, s.Position))
                    .ToList();

                var totalPieces = t.FirstOrDefault()?.Item1;
                var pieceCount = t.Select(s => s.Item2).Distinct().Count();

                if (totalPieces != pieceCount)
                {
                    return false;
                }
            }

            return true;
        }

        // TODO: Think of a better way to do this.
        public Common.MissingPieces GetMissingFilePieces(Common.Manifest manifest)
        {
            using var context = this.factory.Create();

            Func<int[], int, List<int[]>> makeMissingPiece = (pieces, total) =>
            {
                var receivedPositions = pieces.ToHashSet();

                var missing = Enumerable.Range(0, total)
                    .ToHashSet()
                    .Except(receivedPositions)
                    .ToArray();

                return getMissingPositions(missing);
            };

            var groupList = context.FileParts
                .GroupBy(g => new { Filename = g.Filename, FileHash = g.FileHash })
                .AsEnumerable()
                .Select(s => s.GroupBy(g => g.Position))
                .Select(s => s.Select(s1 => s1.First()).ToList())
                .ToList();

            List<MissingPiece> missingPieces = new();

            foreach (var group in groupList)
            {
                var filename = group.First().Filename;
                var fileHash = group.First().FileHash;
                var total = group.First().TotalPieces;

                var positions = group.Select(s => s.Position).ToArray();

                var missing = makeMissingPiece(positions, total);

                missingPieces.Add(new MissingPiece(filename, fileHash, missing));
            }

            List<MissingPiece> output = new();

            foreach (MissingPiece missingPiece in missingPieces)
            {
                var manifestFile = manifest.Files
                    .Where(w => missingPiece.Filename == w.Filename)
                    .Where(w => missingPiece.FileHash == w.FileHash)
                    .FirstOrDefault();

                if (manifestFile == null)
                {
                    // File parts are not in manifest, delete.
                    context.FileParts
                        .Where(w => missingPiece.Filename == w.Filename)
                        .Where(w => missingPiece.FileHash == w.FileHash)
                        .ToList()
                        .ForEach(f => context.Remove(f));
                }
                else
                {
                    if (missingPiece.MissingPositions.Count > 0)
                    {
                        output.Add(missingPiece);
                    }
                }
            }

            context.SaveChanges();

            var missingFiles = output
                .Select(s => (s.Filename, s.FileHash))
                .ToHashSet()
                .Except(manifest.Files.Select(s => (s.Filename, s.FileHash)).ToHashSet())
                .Select(s => new ManifestFile(s.Filename, s.FileHash))
                .ToArray();

            return new MissingPieces(output.ToArray(), missingFiles);
        }

        private static List<int[]> getMissingPositions(int[] missing)
        {
            return getMissingPositions(missing, new List<int[]>());
        }

        private static List<int[]> getMissingPositions(int[] missing, List<int[]> collectorSet)
        {
            if (missing.Length == 0)
            {
                return collectorSet.ToList();
            }

            List<int> newCollection = new();

            for (int i = 0; i < missing.Length; i++)
            {
                if (i == 0)
                {
                    newCollection.Add(missing[i]);
                    continue;
                }
                else if (missing[i] == missing[missing.Length - 1])
                {
                    newCollection.Add(missing[i]);
                    continue;
                }

                if (missing[i] == (missing[i - 1] + 1))
                {
                    newCollection.Add(missing[i]);
                }
                else
                {
                    break;
                }
            }

            collectorSet.Add(newCollection.ToArray());

            return getMissingPositions(missing[newCollection.Count..], collectorSet);
        }

        public string GetFilePiecesByFilenameAndHash(string filename, string fileHash)
        {
            using var context = this.factory.Create();

            var x = context.FileParts
                .Where(w => w.Filename == filename)
                .Where(w => w.FileHash == fileHash)
                .OrderBy(o => o.Position)
                .Select(s => new Tuple<int, string>(s.Position, s.Payload))
                .ToList();

            var y = x
                .Distinct()
                .Select(s => s.Item2)
                .ToList();

            return y.Aggregate((a, b) => a + b);
        }
    }
}
