using Akka.Actor;
using Akka.Event;
using AkkaDistribution.Client.Data;
using AkkaDistribution.Common;

namespace AkkaDistribution.Client.Actors
{
    public class ReceiveFilePartActor : ReceiveActor
    {
        private readonly IFilePartRepository filePartRepository;
        private readonly ILoggingAdapter logger = Context.GetLogger();

        public ReceiveFilePartActor(IFilePartRepository filePartRepository)
        {
            this.filePartRepository = filePartRepository;

            var address = $"akka.tcp://file-receive-system/user/rebuild-file-actor";
            var rebuildFileActor = Context.ActorSelection(address);

            Receive<FilePartMessage>(filePartMessage =>
            {
                this.filePartRepository.Add(filePartMessage);

                rebuildFileActor.Tell(new WakeUp());
                rebuildFileActor.Tell(new ResetTimeout());
            });
        }

        public static Props CreateProps(IFilePartRepository filePartRepository)
        {
            return Props.Create(() => new ReceiveFilePartActor(filePartRepository));
        }
    }
}