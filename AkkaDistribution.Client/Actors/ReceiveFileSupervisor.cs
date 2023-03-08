using Akka.Actor;
using Akka.Event;
using Akka.Routing;
using AkkaDistribution.Common;
using MyApp;

namespace AkkaDistribution.Client.Actors // Note: actual namespace depends on the project name.
{
    public class ReceiveFileSupervisor : ReceiveActor
    {
        private readonly ILoggingAdapter logger = Context.GetLogger();

        public ReceiveFileSupervisor()
        {
            Props rebuildFileProps = Props.Create(() => new RebuildFileActor());
            var rebuildFileActor = Context.ActorOf(rebuildFileProps, "rebuild-file-actor");

            Props receiveFileProps = Props.Create(() => new ReceiveFilePartActor())
                .WithRouter(new RoundRobinPool(5, new DefaultResizer(1, 1000)));
            var receiveFilePartRouter = Context.ActorOf(receiveFileProps);

            Receive<FilePartMessage>(receiveFilePartRouter.Forward);
        }
    }
}