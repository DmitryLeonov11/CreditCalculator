using System.Collections.Concurrent;
using System.Text.RegularExpressions;
using CreditCalculator.Application.Abstractions;

namespace CreditCalculator.Api.IntegrationTests.Fixtures;

public sealed partial class CapturingEmailSender : IEmailSender
{
    private readonly ConcurrentQueue<SentEmail> _sentEmails = new();

    public Task SendAsync(string to, string subject, string body, CancellationToken cancellationToken = default)
    {
        _sentEmails.Enqueue(new SentEmail(to, subject, body));
        return Task.CompletedTask;
    }

    public Uri GetConfirmationLink(string email)
    {
        var sentEmail = _sentEmails.Last(sentEmail => sentEmail.To == email);
        return new Uri(LinkRegex().Match(sentEmail.Body).Value);
    }

    [GeneratedRegex(@"https?://\S+")]
    private static partial Regex LinkRegex();

    private sealed record SentEmail(string To, string Subject, string Body);
}
