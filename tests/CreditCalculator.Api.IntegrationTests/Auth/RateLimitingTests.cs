using System.Net;
using CreditCalculator.Api.IntegrationTests.Fixtures;
using FluentAssertions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;

namespace CreditCalculator.Api.IntegrationTests.Auth;

[Collection(ApiCollection.Name)]
public class RateLimitingTests
{
    private readonly ApiFactory _factory;

    public RateLimitingTests(ApiFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Login_TooManyAttempts_ReturnsTooManyRequests()
    {
        using var limitedFactory = _factory.WithWebHostBuilder(builder =>
            builder.ConfigureAppConfiguration(configuration => configuration.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["RateLimiting:Auth:PermitLimit"] = "2"
            })));
        var client = limitedFactory.CreateClient();
        var email = ApiClientExtensions.UniqueEmail();

        await client.LoginAsync(email);
        await client.LoginAsync(email);
        var response = await client.LoginAsync(email);

        response.StatusCode.Should().Be(HttpStatusCode.TooManyRequests);
    }
}
