using Akka.Actor;
using Akka.Event;
using AkkaDistribution.Client.Data;
using AkkaDistribution.Common;

namespace AkkaDistribution.Client.Actors // Note: actual namespace depends on the project name.
{
    public class RequestFilesActor : ReceiveActor
    {
        private readonly ILoggingAdapter logger = Context.GetLogger();
        private readonly ActorSelection manifestActor;
        private readonly ActorSelection fileTransferActor;
        private readonly IManifestRepository manifestRepository;
        private readonly IFilePartRepository filePartRepository;
        private readonly FileBox filebox;

        public RequestFilesActor(IManifestRepository manifestRepository, IFilePartRepository filePartRepository, FileBox filebox)
        {
            this.manifestRepository = manifestRepository;
            this.filePartRepository = filePartRepository;
            this.filebox = filebox;

            // TODO: load from config
            manifestActor = Context.ActorSelection("akka.tcp://server-actor-system@localhost:8080/user/file-transfer/manifest-actor");
            fileTransferActor = Context.ActorSelection("akka.tcp://server-actor-system@localhost:8080/user/file-transfer");

            Receive<Common.Manifest>(manifest =>
            {
                logger.Info($"Recieved {nameof(Common.Manifest)}");

                HandleManiest(manifest);
            });

            Receive<ManifestRequest>(_ =>
            {
                logger.Info($"Recieved {nameof(ManifestRequest)}");

                var remoteManifest = manifestActor.Ask<Common.Manifest>(new ManifestRequest(), TimeSpan.FromSeconds(5)).Result;

                Context.Self.Tell(remoteManifest);

                HandleManiest(remoteManifest);
            });
        }

        public static event Action<Exception> ShowError;
        public static event Action AlreadyUpToDate;

        protected override void PostRestart(Exception reason)
        {
            logger.Error(reason.Message);

            ShowError?.Invoke(reason);
        }

        private void HandleManiest(Common.Manifest remoteManifest)
        {
            logger.Info($"Recieved Manifest: {remoteManifest.Timestamp}");
            remoteManifest.Files.ToList().ForEach(f => logger.Info($"folderManifest {f.FileHash} - {f.Filename}"));

            this.manifestRepository.SaveManifest(remoteManifest);

            var missingPieces = this.filePartRepository.GetMissingFilePieces(remoteManifest);

            if (remoteManifest.Files.Count == 0)
            {
                var parent = Context.ActorSelection("..");
                parent.Tell(new WakeUp());
            }
            else if (missingPieces.Missing.Length == 0 && missingPieces.MissingFiles.Length > 0)
            {
                fileTransferActor.Tell(new Common.Manifest(DateTime.UtcNow, missingPieces.MissingFiles.ToHashSet()));
            }
            else if (missingPieces.Missing.Length == 0 && missingPieces.MissingFiles.Length == 0)
            {
                var folderManifest = FileHelper.GenerateManifestFromDirectory(filebox);

                var diff = remoteManifest.Compare(folderManifest);

                if (diff.Files.Count > 0)
                {
                    fileTransferActor.Tell(diff);
                }
                else
                {
                    AlreadyUpToDate?.Invoke();
                }
            }
            else
            {
                fileTransferActor.Tell(missingPieces);
            }
        }

        public static Props CreateProps(IManifestRepository manifestRepository, IFilePartRepository filePartRepository, FileBox filebox)
        {
            return Props.Create(() => new RequestFilesActor(manifestRepository, filePartRepository, filebox));
        }
    }
}