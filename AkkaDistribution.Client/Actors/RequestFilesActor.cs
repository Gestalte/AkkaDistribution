using Akka.Actor;
using Akka.Event;
using AkkaDistribution.Common;

namespace AkkaDistribution.Client.Actors // Note: actual namespace depends on the project name.
{
    public class RequestFilesActor : ReceiveActor
    {
        private readonly ILoggingAdapter logger = Context.GetLogger();
        private readonly ActorSelection manifestActor;
        private readonly ActorSelection fileTransferActor;

        public RequestFilesActor()
        {
            // TODO: load from config
            manifestActor = Context.ActorSelection("akka://server-actor-system@localhost:8080/user/file-transfer/manifest-actor");
            fileTransferActor = Context.ActorSelection("akka://server-actor-system@localhost:8080/user/file-transfer");

            Receive<RequestManifest>(_ =>
            {
                logger.Info("Recieved RequestManifest");
                RequestFiles();
            });

            Receive<Manifest>(manifest =>
            {
                logger.Info("Recieved Manifest");
                RequestFiles(manifest);
            });
        }

        private void RequestFiles(Manifest manifest = null!)
        {
            if (manifest == null)
            {
                manifestActor.Ask(new RequestManifest());
            }
            else
            {
                logger.Info($"Manifest: {manifest.Timestamp}");
                manifest.Files.ToList().ForEach(f => logger.Info($"folderManifest {f.FileHash} - {f.Filename}"));

                fileTransferActor.Tell(manifest);
            }
        }
    }
}