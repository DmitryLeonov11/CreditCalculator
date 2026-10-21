using CreditCalculator.Scoring.Messages.Messages;
using CreditCalculator.Scoring.Messages.RabbitMq;
using CreditCalculator.Scoring.Service.Consumers;
using CreditCalculator.Scoring.Service.Scoring;
using Microsoft.Extensions.Hosting;
using RabbitMQ.Client;

namespace CreditCalculator.Scoring.Service;

public class Program
{
    public static async Task Main(string[] args)
    {
        var builder = Host.CreateApplicationBuilder(args);

        builder.Services.AddSingleton<ConnectionFactory>(sp =>
        {
            var factory = new ConnectionFactory
            {
                HostName = builder.Configuration["RabbitMq:Host"] ?? "localhost",
                Port = int.Parse(builder.Configuration["RabbitMq:Port"] ?? "5672"),
                UserName = builder.Configuration["RabbitMq:UserName"] ?? "guest",
                Password = builder.Configuration["RabbitMq:Password"] ?? "guest",
            };
            return factory;
        });

        builder.Services.AddSingleton<IRabbitMqPublisher, RabbitMqPublisher>();
        builder.Services.AddHostedService<ScoringRequestConsumer>();
        builder.Services.AddScoped<IScoringEngine, ScoringEngine>();

        var host = builder.Build();
        await host.RunAsync();
    }
}

public interface IRabbitMqPublisher
{
    Task PublishAsync<T>(string exchange, string routingKey, T message, CancellationToken cancellationToken = default);
}

public sealed class RabbitMqPublisher : IRabbitMqPublisher
{
    private readonly ConnectionFactory _factory;

    public RabbitMqPublisher(ConnectionFactory factory) => _factory = factory;

    public async Task PublishAsync<T>(string exchange, string routingKey, T message, CancellationToken cancellationToken = default)
    {
        using var connection = _factory.CreateConnection();
        using var channel = connection.CreateModel();

        channel.ExchangeDeclare(exchange: exchange, type: "direct", durable: true);
        channel.QueueDeclare(queue: routingKey, durable: true, exclusive: false, autoDelete: false);
        channel.QueueBind(queue: routingKey, exchange: exchange, routingKey: routingKey);

        var body = System.Text.Json.JsonSerializer.SerializeToUtf8Bytes(message);
        channel.BasicPublish(
            exchange: exchange,
            routingKey: routingKey,
            body: body);
    }
}