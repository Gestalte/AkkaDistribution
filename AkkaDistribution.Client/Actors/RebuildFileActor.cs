using Akka.Actor;
using Akka.Event;
using AkkaDistribution.Client;
using AkkaDistribution.Client.Data;
using AkkaDistribution.Common;

namespace AkkaDistribution.Client.Actors // Note: actual namespace depends on the project name.
{
    public class RebuildFileActor : ReceiveActor
    {
        private readonly ILoggingAdapter logger = Context.GetLogger();
        private readonly IScheduler scheduler = Context.System.Scheduler;
        private readonly IManifestRepository manifestRepository;
        private readonly IFilePartRepository filePartRepository;
        private readonly ActorSelection manifestActor;
        private readonly FileBox fileBox;

        private ICancelable schedulerCancel = default!;
        private DateTime timeout = default;

        public RebuildFileActor(IManifestRepository manifestRepository, IFilePartRepository filePartRepository, FileBox fileBox)
        {
            this.manifestRepository = manifestRepository;
            this.filePartRepository = filePartRepository;
            this.fileBox = fileBox;

            manifestActor = Context.ActorSelection("akka.tcp://server-actor-system@localhost:8080/user/file-transfer/manifest-actor");

            logger.Info(Context.Self.Path.ToStringWithAddress());

            Become(Sleeping);
        }

        public void Awake()
        {
            logger.Info("Awake");

            schedulerCancel = scheduler.ScheduleTellRepeatedlyCancelable(0, 10000, Self, new CheckIfTimedOut(), Self);
            logger.Info("Started scheduler");

            Receive<WakeUp>(_ => { logger.Info("Received WakeUp, but already Awake"); });

            Receive<ResetTimeout>(_ =>
            {
                logger.Info("Received ResetTimeout");

                timeout = DateTime.UtcNow;
            });

            Receive<TransferComplete>(_ =>
            {
                logger.Info("Received TransferComplete");
                AllFilesReceived?.Invoke();
                Become(Sleeping);
            });

            Receive<CheckIfTimedOut>(_ =>
            {
                logger.Info("Received CheckIfTimedOut");

                if (DateTime.UtcNow - timeout > TimeSpan.FromSeconds(5.0))
                {
                    logger.Info("Receiving files timed out");

                    var dbManifest = this.manifestRepository.GetNewestManifest();

                    // Check if db file parts match what is described in the manifest.
                    var missingFiles = this.filePartRepository.GetMissingFilePieces(dbManifest);

                    if (missingFiles.Missing.Length != 0)
                    {
                        logger.Info($"Missing File parts: {missingFiles.Missing.Length} files still have missing parts.");

                        manifestActor.Tell(missingFiles);
                    }
                    else
                    {
                        logger.Info($"No missing file parts");

                        foreach (var file in dbManifest.Files)
                        {
                            string base64 = this.filePartRepository.GetFilePiecesByFilenameAndHash(file.Filename, file.FileHash);

                            byte[] newBytes = Convert.FromBase64String(base64);

                            File.WriteAllBytes(Path.Combine(this.fileBox.DirectoryPath, file.Filename), newBytes);
                        }

                        logger.Info($"Files have been rebuilt.");

                        Become(Sleeping);

                        // TODO: Check if built files match the manifest.
                        // TODO: If it some don't, delete the files that are wrong and try them again.
                        // TODO: Delete all file parts from the DB.
                        // TODO: if everything is fine send self TransferComplete
                    }
                }
            });
        }

        public static event Action AllFilesReceived;

        public void Sleeping()
        {
            logger.Info("Sleeping");

            schedulerCancel?.Cancel();
            logger.Info("Cancelled scheduler");

            Receive<WakeUp>(w =>
            {
                logger.Info("Received WakeUp");

                Become(Awake);
            });
        }

        public static Props CreateProps(IManifestRepository manifestRepository, IFilePartRepository filePartRepository, FileBox fileBox)
        {
            return Props.Create(() => new RebuildFileActor(manifestRepository, filePartRepository, fileBox));
        }
    }
}