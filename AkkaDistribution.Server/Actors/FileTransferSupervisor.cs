using Akka.Actor;
using Akka.Event;
using Akka.Routing;
using AkkaDistribution.Common;
using AkkaDistribution.Server.Data;
using Manifest = AkkaDistribution.Common.Manifest;

namespace AkkaDistribution.Server.Actors
{
    internal sealed class FileTransferSupervisor : ReceiveActor
    {
        private readonly FileBox filebox;

        private readonly ILoggingAdapter logger = Context.GetLogger();

        public FileTransferSupervisor
            (FileBox filebox
            , IManifestRepository manifestRepository
            , IFilePartDeliveryRepository filePartDeliveryRepository
            )
        {
            this.filebox = filebox;

            Props manifestProps = Props
                .Create(() => new ManifestActor(this.filebox, manifestRepository, filePartDeliveryRepository));

            var ManifestActor = Context.ActorOf(manifestProps,"manifest-actor");

            Props serverProps = Props
                .Create(() => new ServerActor(this.filebox, manifestRepository, filePartDeliveryRepository))
                .WithRouter(new RoundRobinPool(5, new DefaultResizer(1, 1000)));

            var serverActorRouter = Context.ActorOf(serverProps);

            Receive<MissingPieces>(serverActorRouter.Tell);
            Receive<Manifest>(serverActorRouter.Tell);

        }
    }
}
