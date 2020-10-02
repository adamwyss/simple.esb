# Simple ESB

A simple no frills easy to setup and easy to use enterprise service bus implementation / task queue / saga framework.

I do need a better name.

## Getting Started

### Console App

Add to Program.cs

```csharp

var host = new SimpleEsbBuilder()
    .UseMongoDb("mongodb://localhost:27017", "DatabaseName")
    .UseRabbitMq("amqp://guest@localhost:5672")
    .RegisterHandlers(typeof(Program).GetTypeInfo().Assembly)
    .BlocksWhileRunning()
    .Build();

host.Run();

```

### AspNet Web App

Add to Startup.cs

```csharp

// This method gets called by the runtime. Use this method to add services to the container.
public void ConfigureServices(IServiceCollection services)
{
    services.AddSimpleEsb(b =>
    {
        b.UseRabbitMq("amqp://guest@localhost:5672")
            .UseMongoDb("mongodb://localhost:27017", "DatabaseName")
            .RegisterHandlers(typeof(Startup).GetTypeInfo().Assembly);
    });
}

public void Configure(IApplicationBuilder app)
{
    loggerFactory.AddConsole(Configuration.GetSection("Logging"));
    loggerFactory.AddDebug();

    app.UseMvc()
       .UseSimpleEsb();
}

```


## Interfaces

### Send Messages

```csharp

IServiceBus bus = services.GetService<IServiceBus>();
bus.Send(new MyMessage() { Info = "some important stuff" });

```

### Handle Messages

```csharp
public class MyHandler : IHandle<MyMessage>
{
        public void Handle(MyMessage msg)
        {
            // perform any custom logic here.
        }
}
```

### Sagas and long running tasks

```csharp
public class MySaga : Saga.WithData<MyState>,
                      IStartedBy<MyMessage>,
                      IHandle<OtherMessage>
{
    public void ConfigureStateDataMapping(mapper)
    {
        mapper.Map<MyMessage>(m => m.Id).To(d => d.Id);
        mapper.Map<OtherMessage>(m => m.Id).To(d => d.Id);
    }

    public void Handle<MyMessage>()
    {
        StateData.Running = true;
        // perform any logic here
    }

    public void Handle<OtherMessage>()
    {
        // 
        StateData.Running = false;
        MarkAsComplete();
    }
}
```

### Handle exceptions thrown while processing message

```csharp
public class MyHandler : IHandle<MyMessage>, IHandleAnyException
{
        public void OnHandlerException(object message, Exception exception)
        {
            // any exception will be passed here for custom handling
        }
}
```

### Setup your handler IOC scope

```csharp

public class SecurityUpdate : IPreviewMessage
{
    public void Peek(object message)
    {
        // perform any preprocessing logic necessary before
        // any of the handlers are called.
    }
}

```

## Motivation

I thought it would be a fun project and I would be able to learn a bit.

Other factors:

Nothing available for dotnet core.

Fan of NServiceBus, MassTransit and other ESBs.
