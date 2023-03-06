using Akka.Actor;
using Akka.Event;
using Akka.Util.Internal;
using AkkaDistribution.Common;
using AkkaDistribution.Server.Data;

namespace AkkaDistribution.Server.Actors
{
    internal sealed class ManifestActor : ReceiveActor
    {
        private readonly ILoggingAdapter logger = Context.GetLogger();

        public ManifestActor
            (FileBox filebox
            , IManifestRepository manifestRepository
            , IFilePartDeliveryRepository filePartDeliveryRepository
            )
        {
            Receive<ManifestRequest>(_ =>
            {
                logger.Info($"Received ManifestRequest");

                var folderManifest = FileHelper.GenerateManifestFromDirectory(filebox);

                logger.Info($"folderManifest: {folderManifest.Timestamp}");
                folderManifest.Files.ForEach(f => logger.Info($"folderManifest {f.FileHash} - {f.Filename}"));

                Sender.Tell(folderManifest);
                logger.Info($"Manifest Sent to: " + Sender.Path);

                Self.Tell(new ManifestUpdate(folderManifest));
            });

            Receive<ManifestUpdate>(m =>
            {
                logger.Info($"Received ManifestUpdate");

                var folderManifest = m.Manifest;

                var dbManifest = manifestRepository.GetNewestManifest();

                logger.Info($"dbManifest:  {dbManifest.Timestamp}");
                dbManifest.Files.ForEach(f => logger.Info($"dbManifest {f.FileHash} - {f.Filename}"));

                var difference = FileHelper.Difference(dbManifest, folderManifest);

                logger.Info($"difference: {difference.Timestamp}");
                difference.Files.ForEach(f => logger.Info($"difference {f.FileHash} - {f.Filename}"));

                // TODO: If there is a difference, currenty everything is
                // deleted and recreated, see if there is a better way to do this.
                if (difference.Files.Count != 0)
                {
                    filePartDeliveryRepository.DeleteAllFilePartDeliveries();

                    for (int i = 0; i < folderManifest.Files.Count; i++)
                    {
                        var name = folderManifest.Files.ToArray()[i].Filename;
                        var hash = folderManifest.Files.ToArray()[i].FileHash;
                        var path = Path.Combine(filebox.DirectoryPath, name);

                        var messages = FileHelper.SplitIntoMessages(path, name, hash);

                        filePartDeliveryRepository.OverwriteFilePartDeliveries(messages);
                    }

                    manifestRepository.SaveManifest(folderManifest);

                    logger.Info($"Saved manifest to DB.");
                }
            });
        }
    }
}
