using CreditCalculator.Scoring.Messages.Messages;
using CreditCalculator.Scoring.Messages.RabbitMq;
using CreditCalculator.Scoring.Service.Scoring;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System.Text;
using System.Text.Json;

namespace CreditCalculator.Scoring.Service.Consumers;

public sealed class ScoringRequestConsumer : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<ScoringRequestConsumer> _logger;
    private readonly ConnectionFactory _connectionFactory;

    public ScoringRequestConsumer(
        IServiceProvider serviceProvider,
        ILogger<ScoringRequestConsumer> logger,
        ConnectionFactory connectionFactory)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
        _connectionFactory = connectionFactory;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("ScoringRequestConsumer started.");

        var connection = _connectionFactory.CreateConnection();
        var channel = connection.CreateModel();

        channel.ExchangeDeclare(QueueNames.Exchange, type: "direct", durable: true);
        channel.QueueDeclare(QueueNames.ScoringRequest, durable: true, exclusive: false, autoDelete: false);
        channel.QueueBind(QueueNames.ScoringRequest, QueueNames.Exchange, QueueNames.ScoringRequest);

        channel.QueueDeclare(QueueNames.ScoringResult, durable: true, exclusive: false, autoDelete: false);
        channel.QueueBind(QueueNames.ScoringResult, QueueNames.Exchange, QueueNames.ScoringResult);

        var consumer = new EventingBasicConsumer(channel);
        consumer.Received += (sender, eventArgs) =>
        {
            try
            {
                var json = Encoding.UTF8.GetString(eventArgs.Body.ToArray());
                var request = System.Text.Json.JsonSerializer.Deserialize<ScoringRequest>(json, new System.Text.Json.JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                if (request == null)
                {
                    _logger.LogWarning("Не удалось десериализовать запрос скоринга.");
                    return;
                }

                using var scope = _serviceProvider.CreateScope();
                var engine = scope.ServiceProvider.GetRequiredService<IScoringEngine>();
                var result = engine.Evaluate(request);

                var publisher = scope.ServiceProvider.GetRequiredService<IRabbitMqPublisher>();
                publisher.PublishAsync(QueueNames.Exchange, QueueNames.ScoringResult, result, stoppingToken).GetAwaiter().GetResult();

                _logger.LogInformation("Скоринг выполнен для заявки {ApplicationId}: Score={Score}, Approved={Approved}",
                    request.ApplicationId, result.TotalScore, result.Approved);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка обработки запроса скоринга.");
            }
        };

        channel.BasicConsume(QueueNames.ScoringRequest, autoAck: true, consumer: consumer);

        await Task.Delay(Timeout.Infinite, stoppingToken);
    }
}