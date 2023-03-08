using Akka.Actor;
using Akka.Event;
using Akka.Routing;
using AkkaDistribution.Client.Data;
using AkkaDistribution.Common;

namespace AkkaDistribution.Client.Actors
{
    public class ReceiveFileSupervisor : ReceiveActor
    {
        private readonly ILoggingAdapter logger = Context.GetLogger();        

        public ReceiveFileSupervisor(IManifestRepository manifestRepository,IFilePartRepository filePartRepository,FileBox fileBox)
        {
            Props rebuildFileProps = RebuildFileActor.CreateProps(manifestRepository, filePartRepository, fileBox);
            var rebuildFileActor = Context.ActorOf(rebuildFileProps, "rebuild-file-actor");

            Props receiveFileProps = ReceiveFilePartActor.CreateProps(filePartRepository)
                .WithRouter(new RoundRobinPool(5, new DefaultResizer(1, 1000)));
            var receiveFilePartRouter = Context.ActorOf(receiveFileProps);

            Receive<FilePartMessage>(receiveFilePartRouter.Forward);
        }

        public static Props CreateProps(IManifestRepository manifestRepository, IFilePartRepository filePartRepository, FileBox fileBox)
        {
            return Props.Create(() => new ReceiveFileSupervisor(manifestRepository, filePartRepository, fileBox));
        }
    }
}