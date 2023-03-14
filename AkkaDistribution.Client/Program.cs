using Akka.Actor;
using Akka.Configuration;
using Akka.Event;
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
            var receiveFileCoordinator = actorSystem.ActorOf(receiveSupervisorProps, "receive-file-coordinator-actor");

            var requestFilesActor = actorSystem.ActorSelection("user/receive-file-coordinator-actor/request-file-actor");

            actorSystem.EventStream.Subscribe(receiveFileCoordinator, typeof(DeadLetter));

            RebuildFileActor.AllFilesReceived += () =>
            {
                Console.WriteLine("All files have been received.");
                Console.WriteLine();

                Thread.Sleep(100);
                PrintInstructions(requestFilesActor, actorSystem);
                Thread.Sleep(100);
            };

            RequestFilesActor.ShowError += reason =>
            {
                Console.WriteLine();
                Console.WriteLine(reason);
                Console.WriteLine();
                PrintInstructions(requestFilesActor, actorSystem);
            };

            RequestFilesActor.AlreadyUpToDate += () =>
            {
                Console.WriteLine();
                Console.WriteLine("Already up to date.");
                Console.WriteLine();

                Thread.Sleep(100);
                PrintInstructions(requestFilesActor, actorSystem);
                Thread.Sleep(100);
            };

            Thread.Sleep(100);
            PrintInstructions(requestFilesActor,actorSystem);
            Thread.Sleep(100);

            actorSystem.WhenTerminated.Wait();
        }

        public static void PrintInstructions(ActorSelection requestFilesActor,ActorSystem actorSystem)
        {
            Thread.Sleep(100);

            Console.WriteLine("Enter \"r\" to request file trasfer or \"e\" to exit.");
            var input = Console.ReadLine();
            if (input == "r")
            {
                requestFilesActor.Tell(new ManifestRequest());
            }
            else if (input == "e")
            {
                actorSystem.Terminate();
                Environment.Exit(1);
            }
            else
            {
                Console.WriteLine();
                Console.WriteLine("Incorrect input");
                Console.WriteLine();
                PrintInstructions(requestFilesActor, actorSystem);
            }

            Thread.Sleep(100);
        }
    }
}