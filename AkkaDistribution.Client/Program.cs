using Akka.Actor;
using Akka.Configuration;
using AkkaDistribution.Client;
using AkkaDistribution.Client.Actors;
using AkkaDistribution.Client.Data;
using AkkaDistribution.Common;
using Microsoft.EntityFrameworkCore;

namespace MyApp // Note: actual namespace depends on the project name.
{
    internal class Program
    {
        static void Main(string[] args)
        {
            // Set up DB.

            using (var dbContext = new ClientDbContext())
            {
                if (dbContext.Database.GetPendingMigrations().ToList().Count != 0)
                {
                    dbContext.Database.Migrate();
                }
            }

            // Set up dependencies.

            IClientDbContextFactory factory = new ClientDbContextFactory();

            IManifestRepository manifestRepo = new ManifestRepository(factory);
            IFilePartRepository filePartRepo = new FilePartRepository(factory);

            FileBox fileBox = new("ReceiveBox");


            // Set up actor system.

            var configFile = File.ReadAllText("hocon.conf");
            var config = ConfigurationFactory.ParseString(configFile);

            var actorSystem = ActorSystem.Create("file-receive-system", config);

            var receiveSupervisorProps = ReceiveFileSupervisor.CreateProps(manifestRepo, filePartRepo, fileBox);
            _ = actorSystem.ActorOf(receiveSupervisorProps, "receive-file-coordinator-actor");

            var requestFilesProps = RequestFilesActor.CreateProps(manifestRepo);
            var requestFilesActor = actorSystem.ActorOf(requestFilesProps);

            RebuildFileActor.AllFilesReceived += () => Console.WriteLine("All files have been received.");

            while (true)
            {
                Console.WriteLine("Enter \"r\" to request file trasfer or \"e\" to exit.");
                var input = Console.ReadLine();
                if (input=="r")
                {
                    requestFilesActor.Tell(new ManifestRequest());
                }
                else if (input=="e")
                {
                    break;
                }
                else
                {
                    Console.WriteLine();
                    Console.WriteLine("Incorrect input");
                    Console.WriteLine();
                }
            }

            //actorSystem.WhenTerminated.Wait();
        }
    }
}