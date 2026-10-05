using System.Net;
using System.Net.Http.Json;
using CreditCalculator.Application.Abstractions;
using CreditCalculator.Api.IntegrationTests.Fixtures;
using CreditCalculator.Application.Contracts;
using CreditCalculator.Domain.Enums;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace CreditCalculator.Api.IntegrationTests.Applications;

[Collection(ApiCollection.Name)]
public class ApplicationsTests
{
    private static readonly UpdateProfileRequest ValidProfile = new(
        "Иванов Иван Иванович",
        new DateOnly(1990, 5, 15),
        Gender.Male,
        MonthlyIncome: 2500.50m,
        EmploymentMonths: 36,
        ExistingMonthlyPayments: 300m,
        Dependents: 1);

    private readonly ApiFactory _factory;
    private readonly HttpClient _client;

    public ApplicationsTests(ApiFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task GetApplications_WithoutToken_ReturnsUnauthorized()
    {
        var response = await _client.GetAsync("/api/v1/applications");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task CreateApplication_ReturnsApplicationWithProductRateAndSnapshot()
    {
        var client = await CreateClientWithProfileAsync();
        var product = await GetConsumerProductAsync(client);

        var response = await client.PostAsJsonAsync("/api/v1/applications", new CreateApplicationRequest(product.Id, 20_000m, 24));

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var application = await response.ReadAsAsync<TestApplication>();
        application.Should().NotBeNull();
        application!.CreditProductId.Should().Be(product.Id);
        application.ProductName.Should().Be(product.Name);
        application.Amount.Should().Be(20_000m);
        application.TermMonths.Should().Be(24);
        application.InterestRate.Should().Be(product.BaseRate);
        application.Status.Should().Be("Rejected");
        application.Score.Should().BeInRange(0, 100);
        application.IncomeAtApply.Should().Be(2500.50m);
        application.ExistingPaymentsAtApply.Should().Be(300m);
        application.AgeAtApply.Should().Be(36);
        await using var scope = _factory.Services.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<IAppDbContext>();
        var savedApplication = await dbContext.Applications
            .AsNoTracking()
            .AsSplitQuery()
            .Include(item => item.StatusHistory)
            .Include(item => item.ScoringResults)
            .SingleAsync(item => item.Id == application.Id);
        savedApplication.BirthDateAtApply.Should().Be(new DateOnly(1990, 5, 15));
        savedApplication.GenderAtApply.Should().Be(Gender.Male);
        savedApplication.Score.Should().Be(application.Score);
        savedApplication.ScoringResults.Should().HaveCount(6);
        application.StatusHistory.Select(entry => entry.ToStatus).Should().Equal("Submitted", "Scoring", "Rejected");
        application.StatusHistory[0].FromStatus.Should().Be("Draft");
    }

    [Fact]
    public async Task CreateApplication_WithSameIdempotencyKey_ReturnsTheSameApplication()
    {
        var client = await CreateClientWithProfileAsync();
        var product = await GetConsumerProductAsync(client);
        var request = new CreateApplicationRequest(product.Id, 10_000m, 12);
        var key = Guid.NewGuid().ToString("N");

        var firstResponse = await PostWithIdempotencyKeyAsync(client, request, key);
        var secondResponse = await PostWithIdempotencyKeyAsync(client, request, key);

        firstResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        secondResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var first = await firstResponse.ReadAsAsync<TestApplication>();
        var second = await secondResponse.ReadAsAsync<TestApplication>();
        second!.Id.Should().Be(first!.Id);

        var page = await (await client.GetAsync("/api/v1/applications")).ReadAsAsync<TestPagedApplication>();
        page!.TotalCount.Should().Be(1);
    }

    [Fact]
    public async Task GetApplications_ReturnsOnlyOwnApplications_WithPagination()
    {
        var owner = await CreateClientWithProfileAsync();
        var stranger = await CreateClientWithProfileAsync();
        var product = await GetConsumerProductAsync(owner);

        var firstResponse = await owner.PostAsJsonAsync("/api/v1/applications", new CreateApplicationRequest(product.Id, 10_000m, 12));
        firstResponse.EnsureSuccessStatusCode();
        var first = await firstResponse.ReadAsAsync<TestApplication>();
        var secondResponse = await owner.PostAsJsonAsync("/api/v1/applications", new CreateApplicationRequest(product.Id, 15_000m, 18));
        secondResponse.EnsureSuccessStatusCode();
        var second = await secondResponse.ReadAsAsync<TestApplication>();

        var strangerResponse = await stranger.PostAsJsonAsync("/api/v1/applications", new CreateApplicationRequest(product.Id, 8_000m, 6));
        strangerResponse.EnsureSuccessStatusCode();

        var ownerPage = await (await owner.GetAsync("/api/v1/applications?page=1&pageSize=2")).ReadAsAsync<TestPagedApplication>();
        ownerPage!.Page.Should().Be(1);
        ownerPage.PageSize.Should().Be(2);
        ownerPage.TotalCount.Should().Be(2);
        ownerPage.Items.Select(item => item.Id).Should().Equal([second!.Id, first!.Id]);

        var strangerPage = await (await stranger.GetAsync("/api/v1/applications")).ReadAsAsync<TestPagedApplication>();
        strangerPage!.TotalCount.Should().Be(1);
        strangerPage.Items.Should().NotContain(item => item.Id == first.Id);
    }

    [Fact]
    public async Task GetApplication_OtherClientCannotReadOrWithdrawIt()
    {
        var owner = await CreateClientWithProfileAsync();
        var stranger = await CreateClientWithProfileAsync();
        var product = await GetConsumerProductAsync(owner);
        var createResponse = await owner.PostAsJsonAsync(
            "/api/v1/applications",
            new CreateApplicationRequest(product.Id, 10_000m, 12));
        createResponse.EnsureSuccessStatusCode();
        var application = await createResponse.ReadAsAsync<TestApplication>();
        var applicationId = application!.Id;

        var ownerGetResponse = await owner.GetAsync($"/api/v1/applications/{applicationId}");
        var getResponse = await stranger.GetAsync($"/api/v1/applications/{applicationId}");
        var withdrawResponse = await stranger.PostAsync($"/api/v1/applications/{applicationId}/withdraw", content: null);

        ownerGetResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        getResponse.StatusCode.Should().Be(HttpStatusCode.NotFound);
        withdrawResponse.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task WithdrawApplication_ChangesStatusAndAddsHistory()
    {
        var client = await CreateClientWithProfileAsync();
        var profileResponse = await client.PutAsJsonAsync("/api/v1/profile", ValidProfile with
        {
            MonthlyIncome = 3000m,
            ExistingMonthlyPayments = 0m,
            Dependents = 0
        });
        profileResponse.EnsureSuccessStatusCode();
        var product = await GetConsumerProductAsync(client);
        var createResponse = await client.PostAsJsonAsync(
            "/api/v1/applications",
            new CreateApplicationRequest(product.Id, 45_000m, 60));
        createResponse.EnsureSuccessStatusCode();
        var application = await createResponse.ReadAsAsync<TestApplication>();
        application!.Status.Should().Be("UnderReview");

        var withdrawResponse = await client.PostAsync($"/api/v1/applications/{application.Id}/withdraw", content: null);

        withdrawResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var withdrawn = await withdrawResponse.ReadAsAsync<TestApplication>();
        withdrawn!.Status.Should().Be("Withdrawn");
        withdrawn.StatusHistory.Should().Contain(entry => entry.ToStatus == "Withdrawn");
    }

    [Fact]
    public async Task CreateApplication_IdempotencyKeyTooLong_ReturnsBadRequest()
    {
        var client = await CreateClientWithProfileAsync();
        var product = await GetConsumerProductAsync(client);
        var key = new string('a', 129);

        var response = await PostWithIdempotencyKeyAsync(
            client,
            new CreateApplicationRequest(product.Id, 10_000m, 12),
            key);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task CreateApplication_IdempotencyKeyUsedByAnotherClient_ReturnsNotFound()
    {
        var owner = await CreateClientWithProfileAsync();
        var stranger = await CreateClientWithProfileAsync();
        var product = await GetConsumerProductAsync(owner);
        var key = Guid.NewGuid().ToString("N");
        var request = new CreateApplicationRequest(product.Id, 10_000m, 12);

        var ownerResponse = await PostWithIdempotencyKeyAsync(owner, request, key);
        ownerResponse.StatusCode.Should().Be(HttpStatusCode.Created);

        var strangerResponse = await PostWithIdempotencyKeyAsync(stranger, request, key);

        strangerResponse.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task CreateApplication_SameIdempotencyKeyDifferentBody_ReturnsConflict()
    {
        var client = await CreateClientWithProfileAsync();
        var product = await GetConsumerProductAsync(client);
        var key = Guid.NewGuid().ToString("N");

        var firstResponse = await PostWithIdempotencyKeyAsync(
            client,
            new CreateApplicationRequest(product.Id, 10_000m, 12),
            key);
        firstResponse.StatusCode.Should().Be(HttpStatusCode.Created);

        var secondResponse = await PostWithIdempotencyKeyAsync(
            client,
            new CreateApplicationRequest(product.Id, 11_000m, 12),
            key);

        secondResponse.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task CreateApplication_AmountOutsideProductLimits_ReturnsBadRequest()
    {
        var client = await CreateClientWithProfileAsync();
        var product = await GetConsumerProductAsync(client);

        var response = await client.PostAsJsonAsync("/api/v1/applications", new CreateApplicationRequest(product.Id, product.MinAmount - 1m, 12));

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task CreateApplication_UnknownProduct_ReturnsNotFound()
    {
        var client = await CreateClientWithProfileAsync();

        var response = await client.PostAsJsonAsync("/api/v1/applications", new CreateApplicationRequest(Guid.NewGuid(), 10_000m, 12));

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task CreateApplication_WithoutProfile_ReturnsNotFound()
    {
        var client = _factory.CreateClient();
        client.Authorize(await client.RegisterConfirmedClientAsync(_factory, ApiClientExtensions.UniqueEmail()));
        var product = await GetConsumerProductAsync(client);

        var response = await client.PostAsJsonAsync("/api/v1/applications", new CreateApplicationRequest(product.Id, 10_000m, 12));

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task GetEmployeeApplications_FiltersSortsAndPaginates()
    {
        var client = await CreateClientWithProfileAsync();
        var product = await GetConsumerProductAsync(client);
        var created = new List<TestApplication>();
        foreach (var amount in new[] { 8_000m, 12_000m, 15_000m })
        {
            var response = await client.PostAsJsonAsync(
                "/api/v1/applications",
                new CreateApplicationRequest(product.Id, amount, 24));
            response.EnsureSuccessStatusCode();
            created.Add((await response.ReadAsAsync<TestApplication>())!);
        }

        var employee = await CreateEmployeeClientAsync();
        var from = Uri.EscapeDataString(created[0].CreatedAt.ToString("O"));
        var to = Uri.EscapeDataString(created[^1].CreatedAt.ToString("O"));
        var page = await (await employee.GetAsync(
            $"/api/v1/employee/applications?minAmount=8000&maxAmount=15000&createdFrom={from}&createdTo={to}&minScore=0&maxScore=100&sortBy=Amount&descending=false&page=1&pageSize=2"))
            .ReadAsAsync<TestPagedEmployeeApplication>();

        page.Should().NotBeNull();
        page!.TotalCount.Should().Be(3);
        page.Page.Should().Be(1);
        page.PageSize.Should().Be(2);
        page.Items.Select(item => item.Amount).Should().Equal(8_000m, 12_000m);

        var exactFilter = await (await employee.GetAsync(
            $"/api/v1/employee/applications?minAmount={created[1].Amount}&maxAmount={created[1].Amount}&minScore={created[1].Score}&maxScore={created[1].Score}&status={created[1].Status}&sortBy=CreatedAt&createdFrom={Uri.EscapeDataString(created[1].CreatedAt.ToString("O"))}&createdTo={Uri.EscapeDataString(created[1].CreatedAt.ToString("O"))}"))
            .ReadAsAsync<TestPagedEmployeeApplication>();
        exactFilter!.TotalCount.Should().Be(1);
        exactFilter.Items.Single().Id.Should().Be(created[1].Id);
    }

    [Fact]
    public async Task GetEmployeeApplication_ReturnsScoringBreakdownAndProposedSchedule()
    {
        var client = await CreateClientWithProfileAsync();
        var product = await GetConsumerProductAsync(client);
        var createResponse = await client.PostAsJsonAsync(
            "/api/v1/applications",
            new CreateApplicationRequest(product.Id, 10_000m, 12));
        createResponse.EnsureSuccessStatusCode();
        var application = (await createResponse.ReadAsAsync<TestApplication>())!;

        var employee = await CreateEmployeeClientAsync();
        var response = await employee.GetAsync($"/api/v1/employee/applications/{application.Id}");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var details = await response.ReadAsAsync<TestEmployeeApplicationDetails>();

        details.Should().NotBeNull();
        details!.Application.Id.Should().Be(application.Id);
        details.ScoringResults.Should().HaveCount(6);
        details.ProposedSchedule.Schedule.Payments.Should().HaveCount(application.TermMonths);
        details.ProposedSchedule.FirstPaymentDate.Should().Be(DateOnly.FromDateTime(application.CreatedAt.UtcDateTime).AddMonths(1));
        details.ProposedSchedule.Schedule.Payments[^1].RemainingBalance.Should().Be(0m);
    }

    [Fact]
    public async Task EmployeeApplicationEndpoints_RejectClientRole()
    {
        var client = await CreateClientWithProfileAsync();

        var listResponse = await client.GetAsync("/api/v1/employee/applications");
        var detailResponse = await client.GetAsync($"/api/v1/employee/applications/{Guid.NewGuid()}");

        listResponse.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        detailResponse.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    private async Task<HttpClient> CreateClientWithProfileAsync()
    {
        var client = _factory.CreateClient();
        client.Authorize(await client.RegisterConfirmedClientAsync(_factory, ApiClientExtensions.UniqueEmail()));

        var profileResponse = await client.PutAsJsonAsync("/api/v1/profile", ValidProfile);
        profileResponse.EnsureSuccessStatusCode();

        return client;
    }

    private async Task<HttpClient> CreateEmployeeClientAsync()
    {
        var client = _factory.CreateClient();
        client.Authorize(await client.LoginAndReadTokensAsync(ApiFactory.SeededEmployeeEmail, ApiFactory.SeededEmployeePassword));
        return client;
    }

    private static async Task<CreditProductResponse> GetConsumerProductAsync(HttpClient client)
    {
        var products = await (await client.GetAsync("/api/v1/products")).ReadAsAsync<List<CreditProductResponse>>();
        return products!.First(product => product.Name == "Потребительский «На любые цели»");
    }

    private static async Task<HttpResponseMessage> PostWithIdempotencyKeyAsync(HttpClient client, CreateApplicationRequest request, string key)
    {
        var content = JsonContent.Create(request, options: ApiClientExtensions.JsonOptions);
        content.Headers.Add("Idempotency-Key", key);
        return await client.PostAsync("/api/v1/applications", content);
    }

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
        DateTimeOffset CreatedAt,
        DateTimeOffset UpdatedAt);

    private sealed record TestStatusHistoryEntry(string? FromStatus, string ToStatus);

    private sealed record TestPagedEmployeeApplication(
        int Page,
        int PageSize,
        int TotalCount,
        List<TestEmployeeApplicationListItem> Items);

    private sealed record TestEmployeeApplicationListItem(
        Guid Id,
        decimal Amount,
        string Status,
        int? Score,
        DateTimeOffset CreatedAt);

    private sealed record TestEmployeeApplicationDetails(
        TestApplication Application,
        List<TestEmployeeScoringResult> ScoringResults,
        TestProposedPaymentSchedule ProposedSchedule);

    private sealed record TestEmployeeScoringResult(string RuleCode, string RuleName, bool Passed, int Points, string Details);

    private sealed record TestProposedPaymentSchedule(DateOnly FirstPaymentDate, TestPaymentSchedule Schedule);

    private sealed record TestPaymentSchedule(List<TestPaymentScheduleItem> Payments, decimal TotalPaid, decimal Overpayment);

    private sealed record TestPaymentScheduleItem(
        int Number,
        DateOnly Date,
        decimal Payment,
        decimal InterestPart,
        decimal PrincipalPart,
        decimal RemainingBalance);

    private sealed record TestPagedApplication(
        int Page,
        int PageSize,
        int TotalCount,
        List<TestApplication> Items);
}
