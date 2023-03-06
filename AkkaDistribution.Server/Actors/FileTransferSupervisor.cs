using Akka.Actor;
using Akka.Event;
using Akka.Routing;
using AkkaDistribution.Common;
using AkkaDistribution.Server.Data;
using static Akka.Remote.Transport.FailureInjectorTransportAdapter;
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

            Props manifestProps = ManifestActor
                .CreateProps(this.filebox, manifestRepository, filePartDeliveryRepository);
            var manifestActor = Context.ActorOf(manifestProps,"manifest-actor");

            Props serverProps = ServerActor
                .CreateProps()
                .WithRouter(new RoundRobinPool(5, new DefaultResizer(1, 1000)));
            var serverActorRouter = Context.ActorOf(serverProps);

            Receive<MissingPieces>(serverActorRouter.Tell);
            Receive<Manifest>(serverActorRouter.Tell);
        }
        public static Props CreateProps
            (FileBox filebox
            , IManifestRepository manifestRepo
            , IFilePartDeliveryRepository deliveryRepo
            )
        {   
            return Props.Create(() => 
                new FileTransferSupervisor(filebox, manifestRepo, deliveryRepo));
        }
    }
}
