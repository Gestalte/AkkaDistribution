using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AkkaDistribution.Client
{
    public sealed record WakeUp();
    public sealed record ResetTimeout();
    public sealed record TransferComplete();
    public sealed record CheckIfTimedOut();
}
