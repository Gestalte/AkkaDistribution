using Akka.Actor;
using Akka.Event;
using Akka.Routing;
using AkkaDistribution.Common;
using AkkaDistribution.Server.Data;
using Manifest = AkkaDistribution.Common.Manifest;

namespace AkkaDistribution.Server
{
    internal sealed class FileTransferSupervisor : ReceiveActor
    {
        private readonly FileBox filebox;

        private readonly ILoggingAdapter logger = Logging.GetLogger(Context);

        public FileTransferSupervisor
            (FileBox filebox
            , IManifestRepository manifestRepository
            , IFilePartDeliveryRepository filePartDeliveryRepository
            )
        {
            this.filebox = filebox;

            Props props = Props
                .Create(() => new ServerActor(this.filebox, manifestRepository, filePartDeliveryRepository))
                .WithRouter(new RoundRobinPool(5, new DefaultResizer(1, 1000)));

            var serverActorRouter = Context.ActorOf(props);

            Receive<ManifestRequest>(r =>
            {
                Sender.Tell(serverActorRouter.Ask<Common.Manifest>(r));
            });
            Receive<MissingPieces>(serverActorRouter.Tell);
            Receive<Manifest>(serverActorRouter.Tell);
        }
    }
}
