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
        public static SimpleEsbBuilder UseRabbitMq(this SimpleEsbBuilder builder, string server)
        {
            builder.Services.AddSingleton<IServiceBus, RabbitServiceBus>();
            builder.Services.AddSingleton<RabbitMqOptions, RabbitMqOptions>(x => new RabbitMqOptions {  Server = server } );
            builder.Services.AddSingleton<IStartupSegment, Server>();

            return builder;
        }
    }

    public class RabbitMqOptions
    {
        public string Server { get; set; }
    }
}
