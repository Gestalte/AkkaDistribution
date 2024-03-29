﻿using Akka.Actor;
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
                .CreateProps(manifestRepository,filePartDeliveryRepository)
                .WithRouter(new RoundRobinPool(5, new DefaultResizer(1, 1000)));
            var serverActorRouter = Context.ActorOf(serverProps);

            Receive<MissingPieces>(serverActorRouter.Forward);
            Receive<Manifest>(serverActorRouter.Forward);
            Receive<DeadLetter>(dl => HandleDeadletter(dl));
        }

        private void HandleDeadletter(DeadLetter dl)
        {
            logger.Warning($"DeadLetter captured: {dl.Message}, sender: {dl.Sender}, recipient: {dl.Recipient}");
            Console.WriteLine($"DeadLetter captured: {dl.Message}, sender: {dl.Sender}, recipient: {dl.Recipient}");
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
