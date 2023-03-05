using Akka.Actor;
using Akka.Configuration;
using Akka.Util.Internal;
using AkkaDistribution.Common;
using AkkaDistribution.Server.Data;
using Google.Protobuf.WellKnownTypes;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System.Data;

namespace AkkaDistribution.Server
{
    internal class Program
    {
        static void Main(string[] args)
        {
            using (var dbContext = new ServerDbContext())
            {
                if (dbContext.Database.GetPendingMigrations().ToList().Count != 0)
                {
                    dbContext.Database.Migrate();
                }
            }

            var configFile = File.ReadAllText("hocon.conf");
            var config = ConfigurationFactory.ParseString(configFile);

            var actorSystem = ActorSystem.Create("server-actor-system", config);

            FileBox fileBox = new ("SendFiles");

            IServerDbContextFactory serverDbContextFactory=new ServerDbContextFactory();

            IManifestRepository manifestRepo = new ManifestRepository(serverDbContextFactory);
            IFilePartDeliveryRepository FilePartDeliveryRepo = new FilePartDeliveryRepository(serverDbContextFactory);

            Props props = Props.Create(() => new FileTransferSupervisor(fileBox, manifestRepo, FilePartDeliveryRepo));

            var fileTransferActor= actorSystem.ActorOf(props,"file-transfer");

            Task<Task<Common.Manifest>> manifest = fileTransferActor.Ask<Task<Common.Manifest>>(new ManifestRequest());

            var x = manifest.Result.Result;

            Console.WriteLine(x.Timestamp);
            x.Files.ForEach(f => Console.WriteLine(f.FileHash + " - " + f.Filename));

            while (true)
            {
                Thread.Sleep(100);
            }
        }
    }
}