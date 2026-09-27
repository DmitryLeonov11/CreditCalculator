using CreditCalculator.Application.Contracts;
using CreditCalculator.Application.Services;
using Microsoft.AspNetCore.Mvc;

namespace CreditCalculator.Api.Controllers;

[ApiController]
[Route("api/v1/products")]
public sealed class ProductsController : ControllerBase
{
    private readonly ICreditProductService _creditProductService;

    public ProductsController(ICreditProductService creditProductService)
    {
        _creditProductService = creditProductService;
    }

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<CreditProductResponse>>> GetActive(CancellationToken cancellationToken) =>
        Ok(await _creditProductService.GetActiveAsync(cancellationToken));
}
