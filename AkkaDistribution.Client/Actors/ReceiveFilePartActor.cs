using Akka.Actor;
using Akka.Event;
using AkkaDistribution.Client;
using AkkaDistribution.Common;

namespace AkkaDistribution.Client.Actors // Note: actual namespace depends on the project name.
{
    public class ReceiveFilePartActor : ReceiveActor
    {
        private readonly ILoggingAdapter logger = Context.GetLogger();

        public ReceiveFilePartActor()
        {
            var address = $"akka.tcp://file-receive-system/user/rebuild-file-actor";
            var rebuildFileActor = Context.ActorSelection(address);

            Receive<FilePartMessage>(filePartMessage =>
            {
                // TODO: save to db
                rebuildFileActor.Tell(new WakeUp());
                rebuildFileActor.Tell(new ResetTimeout());
            });
        }
    }
}