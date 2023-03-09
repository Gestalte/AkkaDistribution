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

        private readonly IManifestRepository manifestRepository;
        private readonly IFilePartDeliveryRepository filePartDeliveryRepository;

        private readonly IActorRef sendFilePartActor;

        public IStash Stash { get; set; } = null!;

        private string address = Context.Self.Path.ToStringWithAddress();

        public ServerActor
            (IManifestRepository manifestRepository
            , IFilePartDeliveryRepository filePartDeliveryRepository
            )
        {
            ManifestActor.WaitForSavingManifest += wait =>
            {
                if (wait)
                {
                    Become(NotReady);
                }
                else
                {
                    Become(Ready);
                }
            };

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
            logger.Info($"{this.address} Now Ready");

            Stash?.UnstashAll();

            Receive<Common.Manifest>(manifest =>
            {
                logger.Info($"{this.address} Received Manifest");
                logger.Info($"Sender: {Sender.Path}");

                var dbManifest = this.manifestRepository.GetNewestManifest();

                var difference = FileHelper.Difference(manifest, dbManifest);

                if (difference.Files.Count != 0)
                {
                    Sender.Tell(dbManifest); // TODO: Sender is always FileTransferSupervisor

                    logger.Info($"{this.address} Client had an older manifest, sent them a new one.");
                }
                else
                {
                    var FilePartDeliveries = this.filePartDeliveryRepository.GetFilePartsDeliveriesByManifest(manifest);

                    for (int i = 0; i < FilePartDeliveries.Count; i++)
                    {
                        this.sendFilePartActor.Tell(FilePartDeliveries[i]);
                    }
                }
            });

            Receive<MissingPieces>(missingPieces =>
            {
                logger.Info($"{this.address} Received MissingPieces");

                var dbManifest = this.manifestRepository.GetNewestManifest();

                // TODO: Test manifest against missing pieces send dbManifest if the client has old files.
                // TODO: Get file pieces from db

                List<FilePartDelivery> FilePartDeliveries = new();

                for (int i = 0; i < FilePartDeliveries.Count; i++)
                {
                    this.sendFilePartActor.Tell(FilePartDeliveries[i]);
                }
            });
        }

        public void NotReady()
        {
            logger.Info($"{this.address} Now NotReady");

            Receive<Common.Manifest>(_ => { Stash?.Stash(); });
            Receive<MissingPieces>(_ => { Stash?.Stash(); });
        }

        public static Props CreateProps
            (IManifestRepository manifestRepository
            , IFilePartDeliveryRepository filePartDeliveryRepository
            )
        {
            return Props.Create(() => new ServerActor(manifestRepository, filePartDeliveryRepository));
        }
    }
}
