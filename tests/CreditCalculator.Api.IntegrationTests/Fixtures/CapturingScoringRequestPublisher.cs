using System.Collections.Concurrent;
using CreditCalculator.Api.Services;
using CreditCalculator.Scoring.Messages.Messages;

namespace CreditCalculator.Api.IntegrationTests.Fixtures;

public sealed class CapturingScoringRequestPublisher : IScoringRequestPublisher
{
    private readonly ConcurrentQueue<ScoringRequest> _requests = new();

    public IReadOnlyCollection<ScoringRequest> Requests => _requests;

    public Task PublishAsync(ScoringRequest request, CancellationToken cancellationToken = default)
    {
        _requests.Enqueue(request);
        return Task.CompletedTask;
    }
}
