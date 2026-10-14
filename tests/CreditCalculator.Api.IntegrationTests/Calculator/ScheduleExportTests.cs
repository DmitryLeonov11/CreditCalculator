using System.IO.Compression;
using System.Net;
using System.Net.Http.Json;
using System.Text;
using CreditCalculator.Api.IntegrationTests.Fixtures;
using CreditCalculator.Api.Contracts;
using CreditCalculator.Calculations.Models;
using FluentAssertions;

namespace CreditCalculator.Api.IntegrationTests.Calculator;

[Collection(ApiCollection.Name)]
public sealed class ScheduleExportTests
{
    private readonly HttpClient _client;

    public ScheduleExportTests(ApiFactory factory)
    {
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
}