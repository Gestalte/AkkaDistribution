using Akka.Actor;
using Akka.Event;
using Akka.Util.Internal;
using AkkaDistribution.Common;
using AkkaDistribution.Server.Data;

namespace AkkaDistribution.Server
{
    internal sealed class ServerActor : ReceiveActor
    {
        private readonly ILoggingAdapter logger = Logging.GetLogger(Context);

        public ServerActor
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
                    for (int i = 0; i < folderManifest.Files.Count; i++)
                    {
                        var name = folderManifest.Files.ToArray()[i].Filename;
                        var path = Path.Combine(filebox.DirectoryPath, name);
                        var hash = folderManifest.Files.ToArray()[i].FileHash;

                        var messages = FileHelper.SplitIntoMessages(path, name, hash);

                        filePartDeliveryRepository.OverwriteFilePartDeliveries(messages);
                    }
                }
            });

            Receive<MissingPieces>(_ => { });

            Receive<Common.Manifest>(_ => { });
        }
    }
}
