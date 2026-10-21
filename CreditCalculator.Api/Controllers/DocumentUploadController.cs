using CreditCalculator.Api.Authentication;
using CreditCalculator.Application.Abstractions;
using CreditCalculator.Application.Contracts;
using CreditCalculator.Application.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CreditCalculator.Api.Controllers;

[ApiController]
[Route("api/v1/applications/{applicationId:guid}/documents")]
[Authorize(Policy = AuthorizationPolicies.ClientOnly)]
public sealed class DocumentUploadController : ControllerBase
{
    private readonly IDocumentStorageService _documentStorage;
    private readonly IApplicationService _applicationService;

    public DocumentUploadController(IDocumentStorageService documentStorage, IApplicationService applicationService)
    {
        _documentStorage = documentStorage;
        _applicationService = applicationService;
    }

    [HttpPost]
    public async Task<IActionResult> UploadDocument(
        Guid applicationId,
        IFormFile file,
        string? description,
        CancellationToken cancellationToken)
    {
        if (file.Length == 0)
            return BadRequest("Файл не выбран.");

        var application = await _applicationService.GetByIdAsync(User.GetUserId(), applicationId, cancellationToken);

        var storagePath = await _documentStorage.UploadDocumentAsync(
            applicationId.ToString(),
            file.FileName,
            file.ContentType,
            file.OpenReadStream(),
            cancellationToken);

        return Ok(new
        {
            ApplicationId = applicationId,
            FileName = file.FileName,
            ContentType = file.ContentType,
            SizeBytes = file.Length,
            StoragePath = storagePath,
            Description = description,
            UploadedAt = DateTimeOffset.UtcNow
        });
    }

    [HttpGet("{storagePath}")]
    public async Task<IActionResult> DownloadDocument(
        Guid applicationId,
        string storagePath,
        CancellationToken cancellationToken)
    {
        await _applicationService.GetByIdAsync(User.GetUserId(), applicationId, cancellationToken);

        var stream = await _documentStorage.DownloadDocumentAsync(storagePath, cancellationToken);
        if (stream == null)
            return NotFound("Документ не найден.");

        return File(stream, "application/octet-stream", Path.GetFileName(storagePath));
    }

    [HttpDelete("{storagePath}")]
    public async Task<IActionResult> DeleteDocument(
        Guid applicationId,
        string storagePath,
        CancellationToken cancellationToken)
    {
        await _applicationService.GetByIdAsync(User.GetUserId(), applicationId, cancellationToken);

        var deleted = await _documentStorage.DeleteDocumentAsync(storagePath, cancellationToken);
        if (!deleted)
            return NotFound("Документ не найден.");

        return NoContent();
    }
}