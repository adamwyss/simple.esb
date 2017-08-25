using System;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Builder;

namespace simple.esb.mvc
{
    public static class ServiceExtensions
    {
        public static IServiceCollection AddSimpleEsb(this IServiceCollection collection, Action<SimpleEsbBuilder> setupAction)
        {
            var builder = new SimpleEsbBuilder(collection);
            setupAction(builder);
            builder.Build();

            return collection;
        }

        public static IApplicationBuilder UseSimpleEsb(this IApplicationBuilder builder)
        {
            var startup = builder.ApplicationServices.GetServices<IStartupSegment>();
            foreach (var s in startup)
            {
                // prevent server from recieving messages it doesn't want righ tnow.
                //s.Start();
            }

            return builder;
        }
    }
}
