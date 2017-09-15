using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Microsoft.Extensions.DependencyInjection;

namespace simple.esb.dependencyinjection
{
    public class ServiceHandlerProvider : IHandlerProvider
    {
        private readonly IServiceProvider _provider;

        public ServiceHandlerProvider(IServiceProvider provider)
        {
            _provider = provider;
        }

        public IEnumerable<object> GetHandlers(Type handlerType, object message)
        {
            IServiceScopeFactory factory = _provider.GetRequiredService<IServiceScopeFactory>();
            using (var scope = factory.CreateScope())
            {
                var scopedServices = scope.ServiceProvider;

                var previewActions = scopedServices.GetServices<IPreviewMessage>();
                foreach (var action in previewActions)
                {
                    action.Peek(message);
                }

                return scopedServices.GetServices(handlerType);
            }
        }
    }

    public static class ServiceCollectionExtensions
    {
        public static SimpleEsbBuilder RegisterHandlers(this SimpleEsbBuilder builder, Assembly assembly)
        {
            IServiceCollection services = builder.Services;

            foreach (Type type in assembly.GetTypes())
            {
                foreach (var handlerInterface in type.GetInterfaces().Where(MessageRouter.IsGenericHandler))
                {
                    Type[] genericArgs = handlerInterface.GetGenericArguments();
                    if (genericArgs.Length != 1)
                    {
                        // this is really more of an assert.
                        throw new InvalidOperationException("The IHandle<> interface contains more than one generic argument.");
                    }

                    services.AddTransient(handlerInterface, type);
                }
            }

            return builder;
        }
    }
}
