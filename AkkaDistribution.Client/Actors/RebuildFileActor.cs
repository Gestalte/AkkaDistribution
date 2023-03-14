using Akka.Actor;
using Akka.Event;
using Akka.Util.Internal;
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
        private readonly ActorSelection serverActorCoordinator;
        private readonly FileBox fileBox;

        private ICancelable schedulerCancel = default!;
        private DateTime timeout = default;

        public RebuildFileActor(IManifestRepository manifestRepository, IFilePartRepository filePartRepository, FileBox fileBox)
        {
            this.manifestRepository = manifestRepository;
            this.filePartRepository = filePartRepository;
            this.fileBox = fileBox;

            manifestActor = Context.ActorSelection("akka.tcp://server-actor-system@localhost:8080/user/file-transfer/manifest-actor");
            serverActorCoordinator = Context.ActorSelection("akka.tcp://server-actor-system@localhost:8080/user/file-transfer");

            logger.Info(Context.Self.Path.ToStringWithAddress());

            Become(Sleeping);
        }

        public void Awake()
        {
            logger.Info("Awake");

            schedulerCancel = scheduler.ScheduleTellRepeatedlyCancelable(0, 10000, Self, new CheckIfTimedOut(), Self);
            logger.Info("Started scheduler");

            Receive<WakeUp>(_ => { });

            Receive<ResetTimeout>(_ =>
            {
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

                    if (missingFiles.Missing.Length != 0 || missingFiles.MissingFiles.Length != 0)
                    {
                        logger.Info($"Missing File parts: {missingFiles.Missing.Length} files still have missing parts and {missingFiles.MissingFiles.Length} files are completely missing.");

                        serverActorCoordinator.Tell(missingFiles);
                    }
                    else
                    {
                        logger.Info($"No missing file parts");

                        // Delete whatever is in receiving folder.
                        var filesInFolder = Directory.EnumerateFiles(fileBox.DirectoryPath, "*.*", SearchOption.AllDirectories).ToList();
                        filesInFolder.ForEach(File.Delete);                        

                        // Write files to the receiving folder from the DB.
                        foreach (var file in dbManifest.Files)
                        {
                            string base64 = this.filePartRepository.GetFilePiecesByFilenameAndHash(file.Filename, file.FileHash);

                            byte[] newBytes = Convert.FromBase64String(base64);

                            File.WriteAllBytes(Path.Combine(this.fileBox.DirectoryPath, file.Filename), newBytes);
                        }

                        logger.Info($"Files have been rebuilt.");

                        Self.Tell(new TransferComplete());
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

            Receive<CheckIfTimedOut>(_ =>
            {
                logger.Info($"Received {nameof(CheckIfTimedOut)} while sleeping.");
            });
        }

        public static Props CreateProps(IManifestRepository manifestRepository, IFilePartRepository filePartRepository, FileBox fileBox)
        {
            return Props.Create(() => new RebuildFileActor(manifestRepository, filePartRepository, fileBox));
        }
    }
}