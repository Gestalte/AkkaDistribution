using Akka.Actor;
using Akka.Event;
using AkkaDistribution.Common;
using AkkaDistribution.Server.Data;

namespace AkkaDistribution.Server.Actors
{

    internal sealed class ServerActor : ReceiveActor
    {
        private readonly ILoggingAdapter logger = Context.GetLogger();

        // TODO: Check that manifest is still valid
        // TODO: If its invalid, send a new manifest back.
        // TODO: Select from db
        // TODO: Send to sender.
        public ServerActor() { }

        public void Ready()
        {
            Receive<Common.Manifest>(_ => { });
            Receive<MissingPieces>(_ => { });
        }

        public void NotReady()
        {
            Receive<Common.Manifest>(_ => { });
            Receive<MissingPieces>(_ => { });
        }

        public static Props CreateProps()
        {
            return Props.Create<ServerActor>();
        }
    }
}
