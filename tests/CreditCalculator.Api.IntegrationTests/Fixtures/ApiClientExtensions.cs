using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using CreditCalculator.Application.Contracts;
using FluentAssertions;

namespace CreditCalculator.Api.IntegrationTests.Fixtures;

public static class ApiClientExtensions
{
    public const string ValidPassword = "Password123";

    public static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() }
    };

    public static Task<T?> ReadAsAsync<T>(this HttpResponseMessage response) =>
        response.Content.ReadFromJsonAsync<T>(JsonOptions);

    public static Task<HttpResponseMessage> RegisterAsync(this HttpClient client, string email, string password = ValidPassword) =>
        client.PostAsJsonAsync("/api/v1/auth/register", new RegisterRequest(email, password, PersonalDataConsent: true));

    public static Task<HttpResponseMessage> LoginAsync(this HttpClient client, string email, string password = ValidPassword) =>
        client.PostAsJsonAsync("/api/v1/auth/login", new LoginRequest(email, password));

    public static Task<HttpResponseMessage> RefreshAsync(this HttpClient client, string refreshToken) =>
        client.PostAsJsonAsync("/api/v1/auth/refresh", new RefreshTokenRequest(refreshToken));

    public static async Task<AuthTokensResponse> RegisterConfirmedClientAsync(this HttpClient client, ApiFactory factory, string email)
    {
        (await client.RegisterAsync(email)).EnsureSuccessStatusCode();
        (await client.GetAsync(factory.EmailSender.GetConfirmationLink(email).PathAndQuery)).EnsureSuccessStatusCode();

        return await client.LoginAndReadTokensAsync(email);
    }

    public static async Task<AuthTokensResponse> LoginAndReadTokensAsync(this HttpClient client, string email, string password = ValidPassword)
    {
        var response = await client.LoginAsync(email, password);
        response.EnsureSuccessStatusCode();

        var tokens = await response.ReadAsAsync<AuthTokensResponse>();
        tokens.Should().NotBeNull();
        return tokens!;
    }

    public static void Authorize(this HttpClient client, AuthTokensResponse tokens) =>
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", tokens.AccessToken);

    public static string UniqueEmail() => $"client-{Guid.NewGuid():N}@example.com";
}
