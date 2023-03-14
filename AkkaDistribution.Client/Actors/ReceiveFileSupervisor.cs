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
            // TODO: Maybe roll RebuildFileActor and RequestFilesActor into one FileActor.
            Props rebuildFileProps = RebuildFileActor.CreateProps(manifestRepository, filePartRepository, fileBox);
            var rebuildFileActor = Context.ActorOf(rebuildFileProps, "rebuild-file-actor");

            var requestFilesProps = RequestFilesActor.CreateProps(manifestRepository, filePartRepository, fileBox);
            var requestFilesActor = Context.ActorOf(requestFilesProps,"request-file-actor");

            Props receiveFileProps = ReceiveFilePartActor.CreateProps(filePartRepository)
                .WithRouter(new RoundRobinPool(5, new DefaultResizer(1, 1000)));
            var receiveFilePartRouter = Context.ActorOf(receiveFileProps);

            Receive<Common.Manifest>(requestFilesActor.Forward);

            Receive<FilePartMessage>(receiveFilePartRouter.Forward);

            Receive<WakeUp>(rebuildFileActor.Forward);

            Receive<DeadLetter>(dl => HandleDeadletter(dl));
        }

        private void HandleDeadletter(DeadLetter dl)
        {
            logger.Warning($"DeadLetter captured: {dl.Message}, sender: {dl.Sender}, recipient: {dl.Recipient}");
            Console.WriteLine($"DeadLetter captured: {dl.Message}, sender: {dl.Sender}, recipient: {dl.Recipient}");
        }

        public static Props CreateProps(IManifestRepository manifestRepository, IFilePartRepository filePartRepository, FileBox fileBox)
        {
            return Props.Create(() => new ReceiveFileSupervisor(manifestRepository, filePartRepository, fileBox));
        }
    }
}