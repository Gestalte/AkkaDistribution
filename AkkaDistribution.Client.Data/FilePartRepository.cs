using AkkaDistribution.Common;
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

        // TODO: Check if db file parts match what is described in the manifest.
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

            MissingPiece[] missingPieces = (from n in context.FileParts
                                            group n by new { n.Filename, n.FileHash, n.TotalPieces, n.Position } into m
                                            select new
                                            {
                                                FileName = m.Key.Filename,
                                                m.Key.FileHash,
                                                MissingPieces = makeMissingPiece(m.Select(s => s.Position).ToArray(), m.Key.TotalPieces),
                                            })
                                            .Select(s => new Common.MissingPiece(s.FileName, s.FileHash, s.MissingPieces))
                                            .ToArray();

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

                if (missingPiece.MissingPositions.Count > 0)
                {
                    output.Add(missingPiece);
                }
            }

            context.SaveChanges();

            return new MissingPieces(output.ToArray());
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

        public string GetFilePiecesByFilenameAndHash(string filename,string fileHash)
        {
            using var context = this.factory.Create();

            return context.FileParts
                .Where(w => w.Filename == filename)
                .Where(w => w.FileHash == fileHash)
                .Select(s => s.Payload)
                .OrderBy(o => o)
                .Distinct()
                .Aggregate((a, b) => a + b);
        }
    }
}
