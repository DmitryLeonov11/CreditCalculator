namespace CreditCalculator.Domain.Entities;

public class Document
{
    public Guid Id { get; set; }
    public Guid ApplicationId { get; set; }
    public string FileName { get; set; } = string.Empty;
    public string ContentType { get; set; } = string.Empty;
    public long SizeBytes { get; set; }
    public string StoragePath { get; set; } = string.Empty; // ключ в S3/MinIO
    public string? Description { get; set; }
    public DateTime UploadedAt { get; set; }
    public Application Application { get; set; } = null!;
}