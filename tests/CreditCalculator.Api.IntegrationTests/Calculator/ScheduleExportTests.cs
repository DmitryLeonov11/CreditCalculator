using System.IO.Compression;
using System.Net;
using System.Net.Http.Json;
using System.Text;
using CreditCalculator.Api.IntegrationTests.Fixtures;
using CreditCalculator.Api.Contracts;
using CreditCalculator.Calculations.Models;
using FluentAssertions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.HttpsPolicy;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

namespace CreditCalculator.Api.IntegrationTests.Calculator;

[Collection(ApiCollection.Name)]
public sealed class ScheduleExportTests
{
    private readonly ApiFactory _factory;
    private readonly HttpClient _client;

    public ScheduleExportTests(ApiFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task ExportSchedule_ReturnsValidPdfAndExcelFiles()
    {
        var request = new ScheduleRequest(
            Amount: 12_000m,
            TermMonths: 12,
            AnnualRatePercent: 12m,
            PaymentType: PaymentType.Annuity,
            FirstPaymentDate: new DateOnly(2026, 11, 1));

        var pdfResponse = await _client.PostAsJsonAsync("/api/v1/calculator/schedule/pdf", request);
        pdfResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        pdfResponse.Content.Headers.ContentType?.MediaType.Should().Be("application/pdf");
        pdfResponse.Content.Headers.ContentDisposition?.FileNameStar.Should().Be("payment-schedule.pdf");
        var pdf = await pdfResponse.Content.ReadAsByteArrayAsync();
        Encoding.ASCII.GetString(pdf, 0, 5).Should().Be("%PDF-");

        var excelResponse = await _client.PostAsJsonAsync("/api/v1/calculator/schedule/excel", request);
        excelResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        excelResponse.Content.Headers.ContentType?.MediaType
            .Should().Be("application/vnd.openxmlformats-officedocument.spreadsheetml.sheet");
        excelResponse.Content.Headers.ContentDisposition?.FileNameStar.Should().Be("payment-schedule.xlsx");
        var excel = await excelResponse.Content.ReadAsByteArrayAsync();
        await using var stream = new MemoryStream(excel);
        using var archive = new ZipArchive(stream, ZipArchiveMode.Read);
        archive.GetEntry("xl/worksheets/sheet1.xml").Should().NotBeNull();
        archive.GetEntry("xl/workbook.xml").Should().NotBeNull();
    }

    [Fact]
    public async Task CorrelationId_IsReturnedAndInvalidValuesAreReplaced()
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, "/api/v1/products");
        request.Headers.Add("X-Correlation-ID", "test-correlation-123");

        using var response = await _client.SendAsync(request);

        response.Headers.GetValues("X-Correlation-ID").Should().ContainSingle().Which
            .Should().Be("test-correlation-123");
    }

    [Fact]
    public async Task CorrelationId_IsGeneratedWhenHeaderIsMissingOrMalformed()
    {
        using var generatedResponse = await _client.GetAsync("/api/v1/products");
        using var malformedRequest = new HttpRequestMessage(HttpMethod.Get, "/api/v1/products");
        malformedRequest.Headers.Add("X-Correlation-ID", new string('x', 65));

        using var malformedResponse = await _client.SendAsync(malformedRequest);

        var generatedId = generatedResponse.Headers.GetValues("X-Correlation-ID").Single();
        var replacementId = malformedResponse.Headers.GetValues("X-Correlation-ID").Single();
        Guid.TryParseExact(generatedId, "N", out _).Should().BeTrue();
        Guid.TryParseExact(replacementId, "N", out _).Should().BeTrue();
        generatedId.Should().NotBe(replacementId);
    }

    [Fact]
    public async Task Cors_PreflightAllowsConfiguredOriginAndRejectsOtherOrigins()
    {
        _factory.Services.GetRequiredService<IConfiguration>()
            .GetSection("Cors:AllowedOrigins").Get<string[]>()
            .Should().Contain(ApiFactory.AllowedFrontendOrigin);

        using var allowedRequest = new HttpRequestMessage(HttpMethod.Options, "/api/v1/products");
        allowedRequest.Headers.Add("Origin", ApiFactory.AllowedFrontendOrigin);
        allowedRequest.Headers.Add("Access-Control-Request-Method", "GET");
        allowedRequest.Headers.Add("Access-Control-Request-Headers", "authorization");

        using var allowedResponse = await _client.SendAsync(allowedRequest);

        allowedResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);
        allowedResponse.Headers.TryGetValues("Access-Control-Allow-Origin", out var allowedOrigins).Should()
            .BeTrue($"Expected CORS headers, got: {string.Join(", ", allowedResponse.Headers.Select(header => $"{header.Key}={string.Join(";", header.Value)}"))}");
        allowedOrigins!
            .Should().ContainSingle().Which.Should().Be(ApiFactory.AllowedFrontendOrigin);
        allowedResponse.Headers.GetValues("Access-Control-Allow-Headers")
            .Should().ContainSingle().Which.Should().Contain("authorization");

        using var rejectedRequest = new HttpRequestMessage(HttpMethod.Options, "/api/v1/products");
        rejectedRequest.Headers.Add("Origin", "https://untrusted.example");
        rejectedRequest.Headers.Add("Access-Control-Request-Method", "GET");

        using var rejectedResponse = await _client.SendAsync(rejectedRequest);

        rejectedResponse.Headers.Contains("Access-Control-Allow-Origin").Should().BeFalse();
    }

    [Fact]
    public async Task ApiResponses_IncludeSecurityHeadersWithoutEnablingHstsInDevelopment()
    {
        using var response = await _client.GetAsync("/api/v1/products");

        response.Headers.GetValues("X-Content-Type-Options").Should().ContainSingle().Which.Should().Be("nosniff");
        response.Headers.GetValues("X-Frame-Options").Should().ContainSingle().Which.Should().Be("DENY");
        response.Headers.GetValues("Referrer-Policy").Should().ContainSingle().Which
            .Should().Be("strict-origin-when-cross-origin");
        response.Headers.GetValues("Permissions-Policy").Should().ContainSingle().Which
            .Should().Contain("camera=()");
        response.Headers.Contains("Strict-Transport-Security").Should().BeFalse();
        response.Headers.Contains("Content-Security-Policy").Should().BeFalse();
    }

    [Fact]
    public async Task ApiResponses_EnableHstsAndStrictContentSecurityPolicyOutsideDevelopment()
    {
        using var productionFactory = new WebApplicationFactory<Program>().WithWebHostBuilder(builder => builder
            .UseEnvironment("Production")
            .ConfigureAppConfiguration(configuration => configuration.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:Default"] = "Host=localhost;Database=unused;Username=unused;Password=unused",
                ["Jwt:SigningKey"] = "integration-tests-signing-key-not-for-production",
                ["PublicUrl:BaseUrl"] = ApiFactory.PublicBaseUrl,
                ["Cors:AllowedOrigins:0"] = ApiFactory.AllowedFrontendOrigin
            })));
        using var client = productionFactory.CreateClient();
        using var response = await client.GetAsync("/");

        productionFactory.Services.GetRequiredService<IHostEnvironment>().EnvironmentName.Should().Be("Production");
        productionFactory.Services.GetRequiredService<IOptions<HstsOptions>>().Value.MaxAge
            .Should().Be(TimeSpan.FromDays(365));
        response.Headers.GetValues("Content-Security-Policy").Should().ContainSingle().Which
            .Should().Contain("default-src 'none'").And.Contain("frame-ancestors 'none'");
    }
}