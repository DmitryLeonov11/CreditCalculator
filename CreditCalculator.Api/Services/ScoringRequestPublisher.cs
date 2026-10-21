using CreditCalculator.Api.Contracts;
using CreditCalculator.Scoring.Messages.Messages;
using CreditCalculator.Scoring.Messages.RabbitMq;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;
using System.Text.Json;

namespace CreditCalculator.Api.Services;

public interface IScoringRequestPublisher
{
    Task PublishAsync(ScoringRequest request, CancellationToken cancellationToken = default);
}

public sealed class ScoringRequestPublisher : IScoringRequestPublisher
{
    private readonly ConnectionFactory _factory;
    private readonly ILogger<ScoringRequestPublisher> _logger;

    public ScoringRequestPublisher(
        IOptions<RabbitMqOptions> options,
        ILogger<ScoringRequestPublisher> logger)
    {
        _factory = new ConnectionFactory
        {
            HostName = options.Value.Host,
            Port = options.Value.Port,
            UserName = options.Value.UserName,
            Password = options.Value.Password,
        };
        _logger = logger;
    }

    public async Task PublishAsync(ScoringRequest request, CancellationToken cancellationToken = default)
    {
        try
        {
            using var connection = _factory.CreateConnection();
            using var channel = connection.CreateModel();

            channel.ExchangeDeclare(exchange: QueueNames.Exchange, type: "direct", durable: true);
            channel.QueueDeclare(queue: QueueNames.ScoringRequest, durable: true, exclusive: false, autoDelete: false);
            channel.QueueBind(queue: QueueNames.ScoringRequest, exchange: QueueNames.Exchange, routingKey: QueueNames.ScoringRequest);

            var body = System.Text.Json.JsonSerializer.SerializeToUtf8Bytes(request);
            channel.BasicPublish(
                exchange: QueueNames.Exchange,
                routingKey: QueueNames.ScoringRequest,
                body: body);

            _logger.LogInformation("Запрос скоринга опубликован для заявки {ApplicationId}", request.ApplicationId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ошибка публикации запроса скоринга для заявки {ApplicationId}", request.ApplicationId);
            throw;
        }
    }
}

public sealed class RabbitMqOptions
{
    public const string SectionName = "RabbitMq";
    public string Host { get; set; } = "localhost";
    public int Port { get; set; } = 5672;
    public string UserName { get; set; } = "guest";
    public string Password { get; set; } = "guest";
}