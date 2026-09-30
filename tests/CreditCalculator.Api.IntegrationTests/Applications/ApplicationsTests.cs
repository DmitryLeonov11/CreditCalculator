using System.Net;
using System.Net.Http.Json;
using CreditCalculator.Api.IntegrationTests.Fixtures;
using CreditCalculator.Application.Contracts;
using FluentAssertions;

namespace CreditCalculator.Api.IntegrationTests.Applications;

[Collection(ApiCollection.Name)]
public class ApplicationsTests
{
    private static readonly UpdateProfileRequest ValidProfile = new(
        "Иванов Иван Иванович",
        new DateOnly(1990, 5, 15),
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
        application.Status.Should().Be("Submitted");
        application.IncomeAtApply.Should().Be(2500.50m);
        application.ExistingPaymentsAtApply.Should().Be(300m);
        application.AgeAtApply.Should().Be(36);
        application.StatusHistory.Should().ContainSingle()
            .Which.Should().BeEquivalentTo(new TestStatusHistoryEntry(FromStatus: "Draft", ToStatus: "Submitted"));
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

    private async Task<HttpClient> CreateClientWithProfileAsync()
    {
        var client = _factory.CreateClient();
        client.Authorize(await client.RegisterConfirmedClientAsync(_factory, ApiClientExtensions.UniqueEmail()));

        var profileResponse = await client.PutAsJsonAsync("/api/v1/profile", ValidProfile);
        profileResponse.EnsureSuccessStatusCode();

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
        decimal? IncomeAtApply,
        decimal? ExistingPaymentsAtApply,
        int? AgeAtApply,
        List<TestStatusHistoryEntry> StatusHistory);

    private sealed record TestStatusHistoryEntry(string? FromStatus, string ToStatus);

    private sealed record TestPagedApplication(
        int Page,
        int PageSize,
        int TotalCount,
        List<TestApplication> Items);
}
