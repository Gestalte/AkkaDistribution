using Akka.Actor;
using Akka.Configuration;

namespace MyApp // Note: actual namespace depends on the project name.
{
    internal class Program
    {
        static void Main(string[] args)
        {
            // TODO: Move this to a class anc call that here.
            var configFile = File.ReadAllText("hocon.conf");
            var config = ConfigurationFactory.ParseString(configFile);

            var actorSystem = ActorSystem.Create("file-receive-system", config);

            actorSystem.WhenTerminated.Wait();
        }
    }
}