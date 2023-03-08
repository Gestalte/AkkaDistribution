using Akka.Actor;
using Akka.Event;
using AkkaDistribution.Client;
using AkkaDistribution.Common;

namespace AkkaDistribution.Client.Actors // Note: actual namespace depends on the project name.
{
    public class RebuildFileActor : ReceiveActor
    {
        private readonly ILoggingAdapter logger = Context.GetLogger();
        private readonly IScheduler scheduler = Context.System.Scheduler;

        private ICancelable schedulerCancel = default!;
        private DateTime timeout = default;
        private Manifest manifest = default!;

        public RebuildFileActor()
        {
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
                // TODO: Check the timeout

                if (manifest == null)
                {
                    // TODO: Read the manifest, cache it.
                    //this.manifest = 
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