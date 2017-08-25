# Simple ESB

A simple no frills eazy to setup and easy to use enterprise service bus implementation.

## Motivation

TODO -- Why?

## Getting Started

###Console App

Add to Program.cs

```csharp

var host = new SimpleEsbBuilder()
    .UseMongoDb("mongodb://localhost:27017", "simple_esb_data")
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
        b.UseRabbitMq("amqp://guest@192.168.1.111:5672")
            .UseMongoDb("mongodb://192.168.1.111:27017", "simple_esb", "Data_State_Storage")
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