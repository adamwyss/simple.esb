using Microsoft.Extensions.DependencyInjection;
using simple.esb.mongo;

namespace simple.esb
{
    public static class SimpleEsbBuilderExtensions
    {
        public static SimpleEsbBuilder UseMongoDb(this SimpleEsbBuilder builder, string server, string database)
        {
            builder.Services.AddSingleton<MongoOptions>(p => new MongoOptions { Host = server, Database = database });
            builder.Services.AddSingleton<IDataStore, MongoDoHickey>();

            return builder;
        }
    }

    public class MongoOptions
    {
        public string Host { get; set; }
        public string Database { get; set; }
    }
}
