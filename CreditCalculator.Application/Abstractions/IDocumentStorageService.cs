namespace CreditCalculator.Application.Abstractions;

public interface IDocumentStorageService
{
    Task<string> UploadDocumentAsync(
        string applicationId,
        string fileName,
        string contentType,
        Stream fileStream,
        CancellationToken cancellationToken = default);

    Task<Stream?> DownloadDocumentAsync(
        string storagePath,
        CancellationToken cancellationToken = default);

    Task<bool> DeleteDocumentAsync(
        string storagePath,
        CancellationToken cancellationToken = default);
}

public sealed class S3Options
{
    public const string SectionName = "S3";

    public string Endpoint { get; set; } = "http://localhost:9000";
    public string AccessKey { get; set; } = string.Empty;
    public string SecretKey { get; set; } = string.Empty;
    public string BucketName { get; set; } = "creditcalculator-documents";
}