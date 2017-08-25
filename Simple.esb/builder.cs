using System;
using Microsoft.Extensions.DependencyInjection;
using simple.esb.dependencyinjection;

namespace simple.esb
{
    public class SimpleEsbBuilder
    {
        private IServiceCollection _collection;

        private bool _blockingHost = false;

        public SimpleEsbBuilder()
        {
            _collection = new ServiceCollection();
            ConfigureServices();
        }

        public SimpleEsbBuilder(IServiceCollection collection)
        {
            _collection = collection;
            ConfigureServices();
        }

        public IServiceCollection Services
        {
            get { return _collection; }
        }

        private void ConfigureServices()
        {
            _collection.AddSingleton<IHandlerProvider, ServiceHandlerProvider>();
        }

        public SimpleEsbBuilder BlocksWhileRunning()
        {
            _blockingHost = true;
            return this;
        }

        public SimpleEsbHost Build()
        {
            return new SimpleEsbHost(_collection.BuildServiceProvider(), _blockingHost);
        }
    }

    public class SimpleEsbHost
    {
        private IServiceProvider _provider;
        private bool _blocks;

        public SimpleEsbHost(IServiceProvider provider, bool blocks)
        {
            _provider = provider;
            _blocks = blocks;
        }

        public void Run()
        {
            var startup = _provider.GetServices<IStartupSegment>();
            foreach (var s in startup)
            {
                s.Start();
            }

            if (_blocks)
            {
                Console.WriteLine(" Press [enter] to exit.");
                Console.ReadLine();
            }
        }
    }

    public interface IStartupSegment
    {
        void Start();
    }
}
