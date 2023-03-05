using Akka.Actor;
using Akka.Event;
using AkkaDistribution.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AkkaDistribution.Server
{
    internal sealed class FileTransferSupervisor:ReceiveActor
    {
        private readonly ILoggingAdapter logger = Logging.GetLogger(Context);

        public FileTransferSupervisor()
        {
            Receive<ManifestRequest>(_ => { });
            Receive<MissingPieces>(msg => { });
            Receive<Manifest>(msg=> { });
        }
    }
}
