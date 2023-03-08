using Akka.Actor;
using Akka.Event;
using AkkaDistribution.Client;
using AkkaDistribution.Client.Data;
using AkkaDistribution.Common;

namespace AkkaDistribution.Client.Actors // Note: actual namespace depends on the project name.
{
    public class RebuildFileActor : ReceiveActor
    {
        private readonly ILoggingAdapter logger = Context.GetLogger();
        private readonly IScheduler scheduler = Context.System.Scheduler;
        private readonly IManifestRepository manifestRepository;
        private readonly IFilePartRepository filePartRepository;

        private ICancelable schedulerCancel = default!;
        private DateTime timeout = default;

        public RebuildFileActor(IManifestRepository manifestRepository,IFilePartRepository filePartRepository)
        {
            this.manifestRepository = manifestRepository;
            this.filePartRepository = filePartRepository;

            Become(Sleeping);
        }

        public void Awake()
        {
            logger.Info("Awake");

            schedulerCancel = scheduler.ScheduleTellRepeatedlyCancelable(0, 10000, Self, new CheckIfTimedOut(), Self);
            logger.Info("Started scheduler");

            Receive<WakeUp>(_ => { logger.Info("Received WakeUp, but already Awake"); });

            Receive<ResetTimeout>(_ =>
            {
                logger.Info("Received ResetTimeout");

                timeout = DateTime.UtcNow;
            });

            Receive<TransferComplete>(_ =>
            {
                logger.Info("Received TransferComplete");
                // TODO: Tell the UI that the transfer is done.
                Become(Sleeping);
            });

            Receive<CheckIfTimedOut>(_ =>
            {
                logger.Info("Received CheckIfTimedOut");
              
                if (DateTime.UtcNow - timeout > TimeSpan.FromSeconds(10.0))
                {
                    logger.Info("Receiving files timed out");

                    var dbManifest = this.manifestRepository.GetNewestManifest();

                    // TODO: Check if db file parts match what is described in the manifest.
                    // TODO: If they match build the files
                    // TODO: If they don't prepare a Missing files request.
                    // TODO: Once all files are build, compare them to the manifest to see if it was done correctly.
                    // TODO: If it wasn't, delete the files that are wrong and try them again.
                    // TODO: if everything is fine send self TransferComplete
                }
            });
        }

        public void Sleeping()
        {
            logger.Info("Sleeping");

            schedulerCancel?.Cancel();
            logger.Info("Cancelled scheduler");

            Receive<WakeUp>(w =>
            {
                logger.Info("Received WakeUp");

                Become(Awake);
            });
        }
    }
}