using System.Net;
using System.Net.Http.Json;
using CreditCalculator.Api.IntegrationTests.Fixtures;
using CreditCalculator.Application.Abstractions;
using CreditCalculator.Application.Contracts;
using CreditCalculator.Domain.Enums;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace CreditCalculator.Api.IntegrationTests.Applications;

/// <summary>
///   End-to-end scenario: register → confirm email → fill profile → submit application →
///   auto-scored into UnderReview → seeded employee approves → schedule becomes available.
/// </summary>
[Collection(ApiCollection.Name)]
public class EndToEndScenarioTests
{
    // Profile that yields UnderReview: employment > 24 months, ПДН in 25–40 %.
    private static readonly UpdateProfileRequest UnderReviewProfile = new(
        "Иванов Иван Иванович",
        new DateOnly(1990, 5, 15),
        Gender.Male,
        MonthlyIncome: 3000m,
        EmploymentMonths: 36,
        ExistingMonthlyPayments: 0m,
        Dependents: 0);

    private readonly ApiFactory _factory;
    private readonly HttpClient _client;

    public EndToEndScenarioTests(ApiFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task FullEndToEnd_Register_Profile_Submit_Scored_EmployeeApproves_ScheduleAvailable()
    {
        // 1. Register, confirm email and log in as a client.
        var email = ApiClientExtensions.UniqueEmail();
        _client.Authorize(await _client.RegisterConfirmedClientAsync(_factory, email));

        // 2. Fill in the profile.
        (await _client.PutAsJsonAsync("/api/v1/profile", UnderReviewProfile)).EnsureSuccessStatusCode();

        // 3. Submit an application — the rate comes from the credit product catalog.
        var product = await GetConsumerProductAsync(_client);
        var createResponse = await _client.PostAsJsonAsync(
            "/api/v1/applications",
            new CreateApplicationRequest(product.Id, 45_000m, 60));

        createResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        var application = await createResponse.ReadAsAsync<TestApplication>();
        application.Should().NotBeNull();
        application!.CreditProductId.Should().Be(product.Id);
        application.ProductName.Should().Be(product.Name);
        application.Amount.Should().Be(45_000m);
        application.TermMonths.Should().Be(60);
        application.InterestRate.Should().BeApproximately(product.BaseRate, 0.001m);

        // 4. Auto-scoring lands the application in UnderReview with a scoring breakdown.
        application.Status.Should().Be("UnderReview");
        application.Score.Should().BeInRange(0, 100);
        application.ScoringResults.Should().NotBeEmpty();
        application.StatusHistory.Select(entry => entry.ToStatus)
            .Should().ContainInOrder("Submitted", "Scoring", "UnderReview");
        application.StatusHistory.First().FromStatus.Should().Be("Draft");

        // Scoring results are also persisted in the database.
        await using (var scope = _factory.Services.CreateAsyncScope())
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<IAppDbContext>();
            var saved = await dbContext.Applications
                .AsNoTracking()
                .AsSplitQuery()
                .Include(item => item.StatusHistory)
                .Include(item => item.ScoringResults)
                .SingleAsync(item => item.Id == application.Id);

            saved.ScoringResults.Should().NotBeEmpty();
            saved.StatusHistory.Should().Contain(entry => entry.ToStatus == ApplicationStatus.UnderReview);
        }

        // 5. A seeded employee logs in and approves the application from the queue.
        var employee = _factory.CreateClient();
        employee.Authorize(await employee.LoginAndReadTokensAsync(
            ApiFactory.SeededEmployeeEmail, ApiFactory.SeededEmployeePassword));

        var approveUrl = $"/api/v1/employee/applications/{application.Id}/approve";
        var approveResponse = await employee.PostAsJsonAsync(
            approveUrl,
            new ApproveApplicationRequest(application.Amount, application.TermMonths, application.InterestRate, "Одобрено."));

        approveResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var approved = await approveResponse.ReadAsAsync<TestApplication>();
        approved.Should().NotBeNull();
        approved!.Status.Should().Be("Approved");
        approved.StatusHistory.Select(entry => entry.ToStatus).Should().EndWith("Approved");

        // 6. After approval the payment schedule is available to the application owner.
        var scheduleResponse = await _client.GetAsync($"/api/v1/applications/{application.Id}/schedule");
        scheduleResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var schedule = await scheduleResponse.ReadAsAsync<ScheduleEnvelope>();
        schedule.Should().NotBeNull();
        schedule!.Schedule.Payments.Should().NotBeEmpty();
        schedule.Schedule.Payments.Last().RemainingBalance.Should().BeLessThanOrEqualTo(0.01m);
    }

    private static async Task<CreditProductResponse> GetConsumerProductAsync(HttpClient client)
    {
        var products = await (await client.GetAsync("/api/v1/products")).ReadAsAsync<List<CreditProductResponse>>();
        return products!.First(product => product.Name == "Потребительский «На любые цели»");
    }

    // Local DTOs — the API serializes status enums as camelCase strings; these mirror the
    // real ApplicationResponse / ApplicationScheduleResponse contracts used by the controllers.
    private sealed record TestApplication(
        Guid Id,
        Guid CreditProductId,
        string ProductName,
        decimal Amount,
        int TermMonths,
        decimal InterestRate,
        string Status,
        int? Score,
        decimal? IncomeAtApply,
        decimal? ExistingPaymentsAtApply,
        int? AgeAtApply,
        List<TestStatusHistoryEntry> StatusHistory,
        List<TestScoringResult> ScoringResults,
        DateTimeOffset CreatedAt,
        DateTimeOffset UpdatedAt);

    private sealed record TestStatusHistoryEntry(string? FromStatus, string ToStatus, string? Comment);
    private sealed record TestScoringResult(string RuleCode, string RuleName, bool Passed, int Points, string Details);
    private sealed record ScheduleEnvelope(SchedulePayload Schedule);
    private sealed record SchedulePayload(List<ScheduleItem> Payments);
    private sealed record ScheduleItem(int Number, decimal Payment, decimal InterestPart, decimal PrincipalPart, decimal RemainingBalance);
}