using Akka.Actor;
using Akka.Event;
using AkkaDistribution.Common;
using AkkaDistribution.Server.Data;

namespace AkkaDistribution.Server.Actors
{

    internal sealed class ServerActor : ReceiveActor
    {
        private readonly ILoggingAdapter logger = Context.GetLogger();

        public ServerActor
            (FileBox filebox
            , IManifestRepository manifestRepository
            , IFilePartDeliveryRepository filePartDeliveryRepository
            )
        {
            Receive<Common.Manifest>(manifest =>
            {
                // TODO: Check that manifest is still valid
                // TODO: If its invalid, send a new manifest back.

                // TODO: Select from db
                // TODO: Send to sender.
            });

            Receive<MissingPieces>(_ => { });
        }
    }
}
