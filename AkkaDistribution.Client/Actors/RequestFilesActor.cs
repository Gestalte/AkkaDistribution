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

        public RequestFilesActor(IManifestRepository manifestRepository)
        {
            this.manifestRepository = manifestRepository;

            // TODO: load from config
            manifestActor = Context.ActorSelection("akka.tcp://server-actor-system@localhost:8080/user/file-transfer/manifest-actor");
            fileTransferActor = Context.ActorSelection("akka.tcp://server-actor-system@localhost:8080/user/file-transfer");

            Receive<ManifestRequest>(_ =>
            {
                logger.Info("Recieved RequestManifest");
                RequestFiles();
            });

            Receive<Common.Manifest>(manifest =>
            {
                logger.Info("Recieved Manifest");
                RequestFiles(manifest);
            });
        }

        private void RequestFiles(Common.Manifest manifest = null!)
        {
            if (manifest == null)
            {
                var received = manifestActor.Ask<Common.Manifest>(new ManifestRequest()).Result;

                Context.Self.Tell(received);
            }
            else
            {
                logger.Info($"Manifest: {manifest.Timestamp}");
                manifest.Files.ToList().ForEach(f => logger.Info($"folderManifest {f.FileHash} - {f.Filename}"));

                this.manifestRepository.SaveManifest(manifest);

                fileTransferActor.Tell(manifest);
            }
        }

        public static Props CreateProps(IManifestRepository manifestRepository)
        {
            return Props.Create(() => new RequestFilesActor(manifestRepository));
        }
    }
}