using Akka.Actor;
using Akka.Event;
using Akka.Routing;
using AkkaDistribution.Common;
using AkkaDistribution.Server.Data;

namespace AkkaDistribution.Server.Actors
{
    internal sealed class ServerActor : ReceiveActor, IWithUnboundedStash
    {
        private readonly ILoggingAdapter logger = Context.GetLogger();

        IManifestRepository manifestRepository;
        IFilePartDeliveryRepository filePartDeliveryRepository;

        private readonly IActorRef sendFilePartActor;

        public IStash Stash { get; set; }

        public ServerActor
            (IManifestRepository manifestRepository
            , IFilePartDeliveryRepository filePartDeliveryRepository
            )
        {
            this.manifestRepository = manifestRepository;
            this.filePartDeliveryRepository = filePartDeliveryRepository;

            var props = Props
                .Create<SendFilePartActor>()
                .WithRouter(new RoundRobinPool(5, new DefaultResizer(1, 1000)));

            this.sendFilePartActor = Context.ActorOf(props);

            Become(Ready);
        }

        public void Ready()
        {
            Stash.UnstashAll();

            Receive<Common.Manifest>(manifest =>
            {
                logger.Info($"Received Manifest");

                var dbManifest = this.manifestRepository.GetNewestManifest();

                var difference = FileHelper.Difference(manifest, dbManifest); // TODO: Test this.

                if (difference != null)
                {
                    Sender.Tell(dbManifest);

                    logger.Info($"Client had an older manifest, sent them a new one.");

                    return;
                }

                var FilePartDeliveries = this.filePartDeliveryRepository.GetFilePartsDeliveriesByManifest(manifest);

                for (int i = 0; i < FilePartDeliveries.Count; i++)
                {
                    this.sendFilePartActor.Tell(FilePartDeliveries[i]);
                }
            });

            Receive<MissingPieces>(missingPieces =>
            {
                logger.Info($"Received MissingPieces");

                var dbManifest = this.manifestRepository.GetNewestManifest();

                // TODO: Test manifest agains missing pieces send dbManifest if the client has old files.
                // TODO: Get file pieces from db

                List<FilePartDelivery> FilePartDeliveries = new();

                for (int i = 0; i < FilePartDeliveries.Count; i++)
                {
                    this.sendFilePartActor.Tell(FilePartDeliveries[i]);
                }
            });
        }

        // TODO: While the files pieces are being split and saved stash messages.
        public void NotReady()
        {
            Stash.Stash();

            Receive<Common.Manifest>(_ => { });
            Receive<MissingPieces>(_ => { });
        }

        public static Props CreateProps()
        {
            return Props.Create<ServerActor>();
        }
    }
}
