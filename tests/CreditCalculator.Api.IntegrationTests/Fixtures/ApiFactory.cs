using CreditCalculator.Application.Abstractions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Testcontainers.PostgreSql;

namespace CreditCalculator.Api.IntegrationTests.Fixtures;

public sealed class ApiFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    public const string SeededEmployeeEmail = "employee@creditcalculator.local";
    public const string SeededEmployeePassword = "Employee123";

    private readonly PostgreSqlContainer _database = new PostgreSqlBuilder("postgres:17-alpine").Build();

    public CapturingEmailSender EmailSender { get; } = new();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        // Development — чтобы при старте применились миграции и сидинг, как при локальном запуске.
        builder.UseEnvironment("Development");

        builder.ConfigureAppConfiguration(configuration => configuration.AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["ConnectionStrings:Default"] = _database.GetConnectionString(),
            ["Jwt:SigningKey"] = "integration-tests-signing-key-not-for-production",
            ["RateLimiting:Auth:PermitLimit"] = "1000",
            ["Seed:EmployeeEmail"] = SeededEmployeeEmail,
            ["Seed:EmployeePassword"] = SeededEmployeePassword
        }));

        builder.ConfigureTestServices(services => services.AddSingleton<IEmailSender>(EmailSender));
    }

    public Task InitializeAsync() => _database.StartAsync();

    async Task IAsyncLifetime.DisposeAsync()
    {
        await base.DisposeAsync();
        await _database.DisposeAsync();
    }
}
