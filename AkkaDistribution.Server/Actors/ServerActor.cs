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

                var filePartMessages = this.filePartDeliveryRepository.GetFilePartsDeliveriesByManifest(dbManifest);

                var missingFiles = missingPieces.MissingFiles
                    .Select(s => new ManifestFile(s.Filename, s.FileHash))
                    .ToHashSet();

                var missingFilesManifest = new Common.Manifest(DateTime.UtcNow, missingFiles);

                static bool isPieceValid(MissingPiece piece, List<FilePartMessage> validPieces)
                {
                    // Check that file exists
                    return validPieces
                        .Select(s => s.Filename == piece.Filename && s.FileHash == piece.FileHash)
                        .Any(a => a == false);
                }

                bool validMissing = missingPieces.Missing
                    .Select(s => isPieceValid(s, filePartMessages))
                    .Any(a => a == false);

                // Are there files in the client manifest that are not in the
                // dbManifest or are there pieces in the missing list that are
                // not in the db?
                if (missingFilesManifest.Files.Except(dbManifest.Files).Count() < 0 || validMissing != true)
                {
                    logger.Info($"{this.address} Received invalid Missing pieces, sent back a new manifest");

                    Sender.Tell(dbManifest); // TODO: make sure this is being sent to the right actor.
                }
                else // Missing files are valid
                {
                    logger.Info($"{this.address} Received valid Missing pieces");

                    List<FilePartDelivery> FilePartDeliveries = new();

                    static FilePartDelivery toFilePartDelivery(FilePartMessage m)
                        => new()
                        {
                            Filename = m.Filename,
                            FileHash = m.FileHash,
                            TotalPieces = m.TotalPieces,
                            Position = m.Position,
                            Payload = m.FilePart,
                        };

                    FilePartDeliveries.AddRange(missingPieces.MissingFiles
                         .SelectMany(s => filePartMessages.Where(w => w.Filename == s.Filename))
                        .Select(toFilePartDelivery)
                        .ToArray());

                    FilePartDeliveries.AddRange(missingPieces.Missing
                        .SelectMany(s => filePartMessages.Where(w => w.Filename == s.Filename))
                        .Select(toFilePartDelivery)
                        .ToArray());

                    for (int i = 0; i < FilePartDeliveries.Count; i++)
                    {
                        this.sendFilePartActor.Tell(FilePartDeliveries[i]);
                    }
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
