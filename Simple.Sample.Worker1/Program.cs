using simple.esb;
using System.Reflection;
using simple.esb.dependencyinjection;

namespace Worker
{
    public class Program
    {
        public static void Main(string[] args)
        {
            simple.esb.mongo.MongoHelper.Register<Workflows.TrainModelsData>();
            simple.esb.mongo.MongoHelper.Register<PublishAgentState>();
            simple.esb.mongo.MongoHelper.Register<ImportData>();

            var host = new SimpleEsbBuilder()
                .UseMongoDb("mongodb://192.168.1.111:27017", "simple_esb", "Data_State_Storage")
                .UseRabbitMq("amqp://guest@192.168.1.111:5672")
                .RegisterHandlers(typeof(Program).GetTypeInfo().Assembly)
                .BlocksWhileRunning()
                .Build();

            host.Run();
        }
    }
}
