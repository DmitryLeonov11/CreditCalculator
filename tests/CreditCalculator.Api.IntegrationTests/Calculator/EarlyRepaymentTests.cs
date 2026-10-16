using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using CreditCalculator.Api.Contracts;
using CreditCalculator.Api.IntegrationTests.Fixtures;
using FluentAssertions;

namespace CreditCalculator.Api.IntegrationTests.Calculator;

[Collection(ApiCollection.Name)]
public sealed class EarlyRepaymentTests
{
    private readonly HttpClient _client;

    public EarlyRepaymentTests(ApiFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Theory]
    [InlineData(EarlyRepaymentMode.ReduceTerm)]
    [InlineData(EarlyRepaymentMode.ReducePayment)]
    public async Task EarlyRepaymentComparison_MultiplePaymentsInSameMonth_ReturnsValidSchedules(
        EarlyRepaymentMode mode)
    {
        var request = new EarlyRepaymentRequest(
            Amount: 12_000m,
            TermMonths: 12,
            AnnualRatePercent: 12m,
            FirstPaymentDate: new DateOnly(2026, 11, 1),
            Mode: mode,
            EarlyRepayments: [
                new EarlyRepaymentItemRequest(3, 1_000m),
                new EarlyRepaymentItemRequest(3, 500m),
                new EarlyRepaymentItemRequest(8, 2_000m)
            ]);

        using var response = await _client.PostAsJsonAsync(
            "/api/v1/calculator/early-repayment", request);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        var originalPayments = json.RootElement.GetProperty("original").GetProperty("payments");
        var recalculatedPayments = json.RootElement
            .GetProperty("withEarlyRepayments").GetProperty("payments");

        originalPayments.GetArrayLength().Should().Be(12);
        var repaymentMonth = recalculatedPayments.EnumerateArray()
            .Single(payment => payment.GetProperty("number").GetInt32() == 3);
        repaymentMonth.GetProperty("principalPart").GetDecimal()
            .Should().BeGreaterThan(1_500m);
        recalculatedPayments.EnumerateArray()
            .Should().OnlyContain(payment => payment.GetProperty("remainingBalance").GetDecimal() >= 0m);
        recalculatedPayments.EnumerateArray().Last()
            .GetProperty("remainingBalance").GetDecimal().Should().Be(0m);
    }

    [Theory]
    [InlineData(EarlyRepaymentMode.ReduceTerm)]
    [InlineData(EarlyRepaymentMode.ReducePayment)]
    public async Task EarlyRepaymentComparison_AmountExceedingRemainingBalance_ClosesLoanWithoutNegativeBalance(
        EarlyRepaymentMode mode)
    {
        var request = new EarlyRepaymentRequest(
            Amount: 12_000m,
            TermMonths: 12,
            AnnualRatePercent: 12m,
            FirstPaymentDate: new DateOnly(2026, 11, 1),
            Mode: mode,
            EarlyRepayments: [new EarlyRepaymentItemRequest(3, 50_000m)]);

        using var response = await _client.PostAsJsonAsync(
            "/api/v1/calculator/early-repayment", request);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        var payments = json.RootElement
            .GetProperty("withEarlyRepayments").GetProperty("payments");

        payments.GetArrayLength().Should().Be(3);
        payments.EnumerateArray()
            .Should().OnlyContain(payment => payment.GetProperty("remainingBalance").GetDecimal() >= 0m);
        payments.EnumerateArray().Last()
            .GetProperty("remainingBalance").GetDecimal().Should().Be(0m);
    }
}