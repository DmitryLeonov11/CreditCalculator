using System.Net;
using System.Net.Http.Json;
using CreditCalculator.Api.IntegrationTests.Fixtures;
using CreditCalculator.Application.Abstractions;
using CreditCalculator.Application.Contracts;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace CreditCalculator.Api.IntegrationTests.Auth;

[Collection(ApiCollection.Name)]
public class AuthFlowTests
{
    private readonly ApiFactory _factory;
    private readonly HttpClient _client;

    public AuthFlowTests(ApiFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task Register_NewEmail_ReturnsAcceptedAndSendsConfirmationLink()
    {
        var email = ApiClientExtensions.UniqueEmail();

        var response = await _client.RegisterAsync(email);

        response.StatusCode.Should().Be(HttpStatusCode.Accepted);
        (await response.Content.ReadAsStringAsync()).Should().BeEmpty();
        _factory.EmailSender.GetConfirmationLink(email).AbsolutePath.Should().Be("/api/v1/auth/confirm-email");
    }

    [Fact]
    public async Task Register_ForgedHostHeader_BuildsConfirmationLinkFromConfiguredBaseUrl()
    {
        var email = ApiClientExtensions.UniqueEmail();
        var client = _factory.CreateClient(new WebApplicationFactoryClientOptions { BaseAddress = new Uri("http://attacker.example") });

        await client.RegisterAsync(email);

        _factory.EmailSender.GetConfirmationLink(email).GetLeftPart(UriPartial.Authority).Should().Be(ApiFactory.PublicBaseUrl);
    }

    [Fact]
    public async Task Register_UnconfirmedEmailInDifferentCase_ReturnsAcceptedAndResendsConfirmationLink()
    {
        var email = ApiClientExtensions.UniqueEmail();
        await _client.RegisterAsync(email);

        var response = await _client.RegisterAsync(email.ToUpperInvariant());

        response.StatusCode.Should().Be(HttpStatusCode.Accepted);
        _factory.EmailSender.CountEmails(email).Should().Be(2);
        (await _client.GetAsync(_factory.EmailSender.GetConfirmationLink(email).PathAndQuery)).StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    [Fact]
    public async Task Register_ConfirmedEmail_ReturnsAcceptedWithoutEmailAndKeepsPassword()
    {
        var email = ApiClientExtensions.UniqueEmail();
        await _client.RegisterConfirmedClientAsync(_factory, email);

        var response = await _client.RegisterAsync(email, "OtherPassword1");

        response.StatusCode.Should().Be(HttpStatusCode.Accepted);
        _factory.EmailSender.CountEmails(email).Should().Be(1);
        (await _client.LoginAsync(email, "OtherPassword1")).StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Register_WithoutPersonalDataConsent_ReturnsBadRequest()
    {
        var response = await _client.PostAsJsonAsync(
            "/api/v1/auth/register",
            new RegisterRequest(ApiClientExtensions.UniqueEmail(), ApiClientExtensions.ValidPassword, PersonalDataConsent: false));

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Register_WeakPassword_ReturnsBadRequest()
    {
        var response = await _client.RegisterAsync(ApiClientExtensions.UniqueEmail(), "password");

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task ConfirmEmail_InvalidToken_ReturnsBadRequest()
    {
        var response = await _client.GetAsync("/api/v1/auth/confirm-email?token=forged-token");

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Login_BeforeEmailConfirmed_ReturnsForbidden()
    {
        var email = ApiClientExtensions.UniqueEmail();
        await _client.RegisterAsync(email);

        var response = await _client.LoginAsync(email);

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Login_AfterEmailConfirmed_ReturnsShortLivedAccessTokenAndRefreshToken()
    {
        var email = ApiClientExtensions.UniqueEmail();
        await _client.RegisterAsync(email);
        var confirmResponse = await _client.GetAsync(_factory.EmailSender.GetConfirmationLink(email).PathAndQuery);

        var tokens = await _client.LoginAndReadTokensAsync(email);

        confirmResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);
        tokens.AccessToken.Should().NotBeNullOrEmpty();
        tokens.RefreshToken.Should().NotBeNullOrEmpty();
        tokens.AccessTokenExpiresAt.Should().BeCloseTo(DateTimeOffset.UtcNow.AddMinutes(15), TimeSpan.FromMinutes(1));
        tokens.RefreshTokenExpiresAt.Should().BeAfter(tokens.AccessTokenExpiresAt);
    }

    [Fact]
    public async Task Login_WrongPassword_ReturnsUnauthorized()
    {
        var email = ApiClientExtensions.UniqueEmail();
        await _client.RegisterConfirmedClientAsync(_factory, email);

        var response = await _client.LoginAsync(email, "WrongPassword1");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Login_UnknownEmail_ReturnsUnauthorized()
    {
        var response = await _client.LoginAsync(ApiClientExtensions.UniqueEmail());

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Refresh_ValidToken_ReturnsNewTokenPair()
    {
        var tokens = await _client.RegisterConfirmedClientAsync(_factory, ApiClientExtensions.UniqueEmail());

        var response = await _client.RefreshAsync(tokens.RefreshToken);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var refreshedTokens = await response.ReadAsAsync<AuthTokensResponse>();
        refreshedTokens!.AccessToken.Should().NotBeNullOrEmpty();
        refreshedTokens.RefreshToken.Should().NotBe(tokens.RefreshToken);
    }

    [Fact]
    public async Task Refresh_AlreadyUsedToken_ReturnsUnauthorized()
    {
        var tokens = await _client.RegisterConfirmedClientAsync(_factory, ApiClientExtensions.UniqueEmail());
        await _client.RefreshAsync(tokens.RefreshToken);

        var response = await _client.RefreshAsync(tokens.RefreshToken);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Refresh_UnknownToken_ReturnsUnauthorized()
    {
        var response = await _client.RefreshAsync("unknown-refresh-token");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task RefreshedAccessToken_GrantsAccessToProtectedEndpoint()
    {
        var tokens = await _client.RegisterConfirmedClientAsync(_factory, ApiClientExtensions.UniqueEmail());
        var refreshedTokens = await (await _client.RefreshAsync(tokens.RefreshToken)).ReadAsAsync<AuthTokensResponse>();
        _client.Authorize(refreshedTokens!);

        var response = await _client.GetAsync("/api/v1/profile");

        // Анкета ещё не заполнена, но до контроллера запрос дошёл — токен принят.
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Refresh_SameTokenInParallel_OnlyOneRequestSucceeds()
    {
        var tokens = await _client.RegisterConfirmedClientAsync(_factory, ApiClientExtensions.UniqueEmail());

        var responses = await Task.WhenAll(Enumerable.Range(0, 10).Select(_ => _client.RefreshAsync(tokens.RefreshToken)));

        responses.Count(response => response.StatusCode == HttpStatusCode.OK).Should().Be(1);
        responses.Where(response => response.StatusCode != HttpStatusCode.OK)
            .Should().OnlyContain(response => response.StatusCode == HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Register_SameEmailInParallel_CreatesUserOnceAndAcceptsAllRequests()
    {
        var email = ApiClientExtensions.UniqueEmail();

        var responses = await Task.WhenAll(Enumerable.Range(0, 10).Select(_ => _client.RegisterAsync(email)));

        responses.Should().OnlyContain(response => response.StatusCode == HttpStatusCode.Accepted);
        await using var scope = _factory.Services.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<IAppDbContext>();
        (await dbContext.Users.CountAsync(user => user.Email == email)).Should().Be(1);
    }

    [Fact]
    public async Task Refresh_RemovesRevokedTokensOfUser()
    {
        var email = ApiClientExtensions.UniqueEmail();
        var tokens = await _client.RegisterConfirmedClientAsync(_factory, email);
        (await _client.RefreshAsync(tokens.RefreshToken)).EnsureSuccessStatusCode();

        await using var scope = _factory.Services.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<IAppDbContext>();
        var storedTokens = await dbContext.RefreshTokens.CountAsync(token => token.User.Email == email);

        storedTokens.Should().Be(1);
    }
}
