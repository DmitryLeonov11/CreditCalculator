using System.Net;
using System.Net.Http.Json;
using CreditCalculator.Api.IntegrationTests.Fixtures;
using CreditCalculator.Application.Contracts;
using CreditCalculator.Domain.Enums;
using FluentAssertions;

namespace CreditCalculator.Api.IntegrationTests.Profiles;

[Collection(ApiCollection.Name)]
public class ProfileTests
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

    public ProfileTests(ApiFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task GetProfile_WithoutToken_ReturnsUnauthorized()
    {
        var response = await _client.GetAsync("/api/v1/profile");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task UpdateProfile_ThenGet_ReturnsSavedValues()
    {
        _client.Authorize(await _client.RegisterConfirmedClientAsync(_factory, ApiClientExtensions.UniqueEmail()));

        var updateResponse = await _client.PutAsJsonAsync("/api/v1/profile", ValidProfile);
        var profile = await (await _client.GetAsync("/api/v1/profile")).ReadAsAsync<ProfileResponse>();

        updateResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        profile.Should().BeEquivalentTo(ValidProfile);
    }

    [Fact]
    public async Task UpdateProfile_UnderageClient_ReturnsBadRequest()
    {
        _client.Authorize(await _client.RegisterConfirmedClientAsync(_factory, ApiClientExtensions.UniqueEmail()));
        var underage = ValidProfile with { BirthDate = DateOnly.FromDateTime(DateTime.UtcNow).AddYears(-17) };

        var response = await _client.PutAsJsonAsync("/api/v1/profile", underage);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task GetProfile_AsEmployee_ReturnsForbidden()
    {
        _client.Authorize(await _client.LoginAndReadTokensAsync(ApiFactory.SeededEmployeeEmail, ApiFactory.SeededEmployeePassword));

        var response = await _client.GetAsync("/api/v1/profile");

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }
}
