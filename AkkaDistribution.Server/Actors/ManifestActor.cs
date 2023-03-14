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

        public static event Action<bool>? WaitForSavingManifest;

        public ManifestActor
            (FileBox filebox
            , IManifestRepository manifestRepository
            , IFilePartDeliveryRepository filePartDeliveryRepository
            )
        {
            // TODO: Its possible for for subsequent requests to get old manifests while the new one is being created.
            Receive<ManifestRequest>(_ =>
            {
                logger.Info($"Received ManifestRequest");

                var folderManifest = FileHelper.GenerateManifestFromDirectory(filebox);

                logger.Info($"folderManifest: {folderManifest.Timestamp}");
                folderManifest.Files.ForEach(f => logger.Info($"folderManifest {f.FileHash} - {f.Filename}"));

                var dbManifest = manifestRepository.GetNewestManifest();

                logger.Info($"dbManifest:  {dbManifest.Timestamp}");
                dbManifest.Files.ForEach(f => logger.Info($"dbManifest {f.FileHash} - {f.Filename}"));

                var difference = FileHelper.Difference(dbManifest, folderManifest);

                logger.Info($"difference: {difference.Timestamp}");
                difference.Files.ForEach(f => logger.Info($"difference {f.FileHash} - {f.Filename}"));

                // TODO: Find a better way to do this for performance.
                if (difference.Files.Count != 0)
                {
                    WaitForSavingManifest?.Invoke(true);

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

                    WaitForSavingManifest?.Invoke(false);
                }

                Sender.Tell(folderManifest);
                logger.Info($"Manifest Sent to: " + Sender.Path);
            });
        }

        public static event Action<Exception> ShowError;

        protected override void PostRestart(Exception reason)
        {
            logger.Error(reason.Message);

            ShowError?.Invoke(reason);
        }

        public static Props CreateProps
            (FileBox filebox, IManifestRepository manifestRepository, IFilePartDeliveryRepository filePartDeliveryRepository)
            => Props.Create(() => new ManifestActor(filebox, manifestRepository, filePartDeliveryRepository));
    }
}
