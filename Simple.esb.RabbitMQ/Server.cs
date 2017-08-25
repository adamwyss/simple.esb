using Newtonsoft.Json;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Diagnostics;

namespace simple.esb.rabbitmq
{
    internal class Envelope
    {
        private Envelope()
        {
        }

        public string Type { get; set; }
        public string SerializedData { get; set; }

        public static Envelope Enclose(object message)
        {
            var envelope = new Envelope();
            envelope.Type = message.GetType().AssemblyQualifiedName;
            envelope.SerializedData = JsonConvert.SerializeObject(message);
            return envelope;
        }

        public object Open()
        {
            Type messageType = System.Type.GetType(Type);
            if (messageType == null)
            {
                throw new InvalidOperationException("unknown type, unable to open message envelope");
            }
            
            return JsonConvert.DeserializeObject(SerializedData, messageType);
        }
    }

    public class RabbitServiceBus : IServiceBus
    {
        private RabbitMqOptions _options;

        public RabbitServiceBus(RabbitMqOptions options)
        {
            _options = options;
        }

        public void Retry<T>(T message, TimeSpan delay)
        {
            // hack - figure out how to do this with rabbitmq
            Task.WaitAll(
                Task.Delay(delay)
            );

            Send(message);
        }

        public void Send<T>(T message)
        {
            var factory = new ConnectionFactory() { Uri = _options.Server };
            using (var connection = factory.CreateConnection())
            {
                using (var channel = connection.CreateModel())
                {
                    channel.QueueDeclare(queue: Server.QueueName,
                                         durable: false,
                                         exclusive: false,
                                         autoDelete: false,
                                         arguments: null);

                    var routingKey = Server.RoutingKey;

                    var envelope = Envelope.Enclose(message);
                    var serializedData = JsonConvert.SerializeObject(envelope);
                    var body = Encoding.UTF8.GetBytes(serializedData);
                    
                    channel.BasicPublish(exchange: "",
                                         routingKey: routingKey,
                                         basicProperties: null,
                                         body: body);
                }
            }
        }


    }

    public class Server : IStartupSegment
    {
        public const string QueueName = "hello2";

        public const string RoutingKey = "hello2";

        private readonly IServiceProvider _provider;
        private readonly IHandlerProvider _provider2;
        private readonly IDataStore _dataStore;
        private readonly RabbitMqOptions _options;

        private IConnection _connection;
        private IModel _channel;

        public Server(IServiceProvider provider, IHandlerProvider provider2, IDataStore dataStore, RabbitMqOptions options)
        {
            _provider = provider;
            _provider2 = provider2;
            _dataStore = dataStore;
            _options = options;
        }

        public void Start()
        {
            var factory = new ConnectionFactory() { Uri = _options.Server };
            _connection = factory.CreateConnection();
            _channel = _connection.CreateModel();

            _channel.QueueDeclare(queue: QueueName,
                                    durable: false,
                                    exclusive: false,
                                    autoDelete: false,
                                    arguments: null);
                    
            var consumer = new EventingBasicConsumer(_channel);
            consumer.Received += MessageReceived;
            _channel.BasicConsume(queue: QueueName,
                                    noAck: true,
                                    consumer: consumer);
        }

        private int _active = 0;

        private async void MessageReceived(object sender, BasicDeliverEventArgs e)
        {
            try
            {
                await RouteMessage(e.Body);
            }
            catch (Exception ex)
            {
                // TODO: invoke unhandled exception handler,
                // and eat exception

                Exception descender = ex;
                int depth = 1;
                while (descender != null)
                {
                    Console.WriteLine("*** EXCEPTION {0} ****", depth);
                    Console.WriteLine(descender.Message);
                    Console.WriteLine(descender.StackTrace);

                    descender = descender.InnerException;
                    depth++;
                }
            }
            finally
            {
                // _channel.BasicAck(e.DeliveryTag, false);
            }
        }

        private async Task RouteMessage(byte[] body)
        {
            Interlocked.Increment(ref _active);

            var serializedData = Encoding.UTF8.GetString(body);
            var envelope = JsonConvert.DeserializeObject<Envelope>(serializedData);

            var messageObject = envelope.Open();

            Console.WriteLine("[{0,3}] Received {1}.\n    {2}", _active, messageObject.GetType().Name, envelope.SerializedData);

            var router = new MessageRouter(_provider, _provider2, _dataStore);
            Stopwatch timer = new Stopwatch();
            timer.Start();
            await router.RouteToHandlers(messageObject);
            timer.Stop();

            Interlocked.Decrement(ref _active);

            Console.WriteLine("[{0,3}] Completed {1}.  [{2}]\n    {3}", _active, messageObject.GetType().Name, timer.Elapsed, envelope.SerializedData);
        }
    }
}
