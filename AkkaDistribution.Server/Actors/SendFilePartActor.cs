using Akka.Actor;
using Akka.Event;
using AkkaDistribution.Common;

namespace AkkaDistribution.Server.Actors
{
    internal class SendFilePartActor : ReceiveActor
    {
        private readonly ILoggingAdapter logger = Context.GetLogger();

        public SendFilePartActor()
        {
            var port = Context.System.Settings.Config.GetInt("akka.send.port");
            var hostname = Context.System.Settings.Config.GetString("akka.send.hostname");

            // TODO: Get sender address from parent so that you don't need to include hocon config.
            Receive<FilePartMessage>(message =>
            {
                logger.Info("Received FilePartMessage");

                var address = $"akka.tcp://file-receive-system@{hostname}:{port}/user/receive-file-coordinator-actor";

                var receiveActor = Context.ActorSelection(address);

                receiveActor.Tell(message);

                logger.Info($"Sent part: {message.Position} or {message.TotalPieces}");
            });
        }
    }
}
