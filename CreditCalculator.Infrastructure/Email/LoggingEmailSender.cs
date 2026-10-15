using CreditCalculator.Application.Abstractions;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace CreditCalculator.Infrastructure.Email;

// Почтовый сервер на этом этапе не настраивается — письмо уходит в лог.
// Содержимое письма не пишется в лог: оно может содержать ссылку с токеном подтверждения.
public sealed class LoggingEmailSender : IEmailSender
{
    private readonly ILogger<LoggingEmailSender> _logger;
    private readonly IHostEnvironment _environment;

    public LoggingEmailSender(ILogger<LoggingEmailSender> logger, IHostEnvironment environment)
    {
        _logger = logger;
        _environment = environment;
    }

    public Task SendAsync(string to, string subject, string body, CancellationToken cancellationToken = default)
    {
        if (_environment.IsDevelopment())
        {
            if (subject.StartsWith("Изменение статуса заявки", StringComparison.Ordinal))
                _logger.LogInformation("Письмо для {To} сформировано. Тема: {Subject}. Текст: {Body}", to, subject, body);
            else
                _logger.LogInformation("Письмо для {To} сформировано. Тема: {Subject}", to, subject);
        }
        else
            _logger.LogWarning("Письмо «{Subject}» не отправлено: почтовый сервер не настроен", subject);

        return Task.CompletedTask;
    }
}
