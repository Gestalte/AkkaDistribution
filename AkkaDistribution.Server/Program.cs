using Akka.Actor;
using Akka.Configuration;

namespace AkkaDistribution.Server
{
    internal class Program
    {
        static void Main(string[] args)
        {
            var configFile = File.ReadAllText("hocon.conf");
            var config = ConfigurationFactory.ParseString(configFile);

            var actorSystem = ActorSystem.Create("server-actor-system", config);
        }
    }
}