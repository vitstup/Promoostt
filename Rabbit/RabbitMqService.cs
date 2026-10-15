using System.Text;
using System.Text.Json;
using RabbitMQ.Client;

namespace PromoosTT.Rabbit;

public class RabbitMqService : IAsyncDisposable
{
    private const string Exchange = "task.events";

    private readonly IConnection connection;
    private readonly IChannel channel;
    private readonly SemaphoreSlim gate = new(1, 1);

    public RabbitMqService(IConfiguration cfg)
    {
        var factory = new ConnectionFactory
        {
            HostName = cfg["Rabbit:Host"] ?? "localhost",
            UserName = cfg["Rabbit:User"] ?? "guest",
            Password = cfg["Rabbit:Pass"] ?? "guest"
        };

        connection = factory.CreateConnectionAsync().GetAwaiter().GetResult();
        channel = connection.CreateChannelAsync().GetAwaiter().GetResult();

        channel.ExchangeDeclareAsync(Exchange, ExchangeType.Topic, durable: true, autoDelete: false)
               .GetAwaiter().GetResult();
    }

    public async Task PublishAsync<T>(T evt, string routingKey) where T : class
    {
        try
        {
            var body = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(evt));

            var props = new BasicProperties
            {
                Persistent = true,
                ContentType = "application/json"
            };

            await gate.WaitAsync();
            try
            {
                await channel.BasicPublishAsync(
                    exchange: Exchange,
                    routingKey: routingKey,
                    mandatory: false,
                    basicProperties: props,
                    body: body);
            }
            finally
            {
                gate.Release();
            }
        }
        catch
        {
            // что то делать, к примеру выводить лог
        }
    }

    public async ValueTask DisposeAsync() 
    {
        gate.Dispose();
        await channel.DisposeAsync();
        await connection.DisposeAsync();
    }
}