using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using simple.esb.rabbitmq;

namespace simple.esb
{
    public static class SimpleEsbBuilderExtensions
    {
        public static SimpleEsbBuilder UseRabbitMq(this SimpleEsbBuilder builder, string server, string exchange = "default")
        {
            builder.Services.AddSingleton<IServiceBus, RabbitServiceBus>();
            builder.Services.AddSingleton<RabbitMqOptions, RabbitMqOptions>(x => new RabbitMqOptions {  Server = server, Exchange = exchange } );
            builder.Services.AddSingleton<IStartupSegment, Server>();
            builder.Services.AddSingleton<RabbitClient>();

            return builder;
        }
    }

    public class RabbitMqOptions
    {
        public string Server { get; set; }
        public string Exchange { get; set; }
    }
}
