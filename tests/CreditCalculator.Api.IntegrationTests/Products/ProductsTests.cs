using System.Net;
using CreditCalculator.Api.IntegrationTests.Fixtures;
using CreditCalculator.Application.Contracts;
using CreditCalculator.Domain.Enums;
using FluentAssertions;

namespace CreditCalculator.Api.IntegrationTests.Products;

[Collection(ApiCollection.Name)]
public class ProductsTests
{
    private readonly HttpClient _client;

    public ProductsTests(ApiFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task GetProducts_ReturnsSeededProductsWithoutPrecisionLoss()
    {
        var response = await _client.GetAsync("/api/v1/products");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var products = await response.ReadAsAsync<List<CreditProductResponse>>();
        products.Should().NotBeEmpty();
        products!.Select(product => product.Purpose).Should().Contain([CreditPurpose.Consumer, CreditPurpose.Auto, CreditPurpose.Mortgage]);
        products.Should().Contain(product => product.Name == "Автокредит" && product.BaseRate == 13.9m);
    }
}
