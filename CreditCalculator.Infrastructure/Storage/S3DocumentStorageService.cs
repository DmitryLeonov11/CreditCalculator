using Amazon.S3;
using Amazon.S3.Model;
using CreditCalculator.Application.Abstractions;
using Microsoft.Extensions.Options;

namespace CreditCalculator.Infrastructure.Storage;

public sealed class S3DocumentStorageService : IDocumentStorageService
{
    private readonly IAmazonS3 _s3Client;
    private readonly S3Options _options;

    public S3DocumentStorageService(IAmazonS3 s3Client, IOptions<S3Options> options)
    {
        _s3Client = s3Client;
        _options = options.Value;
    }

    public async Task<string> UploadDocumentAsync(
        string applicationId,
        string fileName,
        string contentType,
        Stream fileStream,
        CancellationToken cancellationToken = default)
    {
        var key = $"applications/{applicationId}/{Guid.NewGuid():N}/{fileName}";

        await EnsureBucketExistsAsync(cancellationToken);

        var request = new PutObjectRequest
        {
            BucketName = _options.BucketName,
            Key = key,
            ContentType = contentType,
            InputStream = fileStream
        };
        request.Metadata.Add("x-amz-meta-application-id", applicationId);
        request.Metadata.Add("x-amz-meta-uploaded-at", DateTimeOffset.UtcNow.ToString("O"));

        await _s3Client.PutObjectAsync(request, cancellationToken);
        return key;
    }

    public async Task<Stream?> DownloadDocumentAsync(
        string storagePath,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var response = await _s3Client.GetObjectAsync(_options.BucketName, storagePath, cancellationToken);
            return response.ResponseStream;
        }
        catch (AmazonS3Exception ex) when (ex.ErrorCode == "NoSuchKey")
        {
            return null;
        }
    }

    public async Task<bool> DeleteDocumentAsync(
        string storagePath,
        CancellationToken cancellationToken = default)
    {
        var request = new DeleteObjectRequest
        {
            BucketName = _options.BucketName,
            Key = storagePath
        };
        var response = await _s3Client.DeleteObjectAsync(request, cancellationToken);
        return response.HttpStatusCode == System.Net.HttpStatusCode.NoContent ||
               response.HttpStatusCode == System.Net.HttpStatusCode.OK;
    }

    private async Task EnsureBucketExistsAsync(CancellationToken cancellationToken)
    {
        var bucketExists = await _s3Client.DoesS3BucketExistAsync(_options.BucketName);
        if (!bucketExists)
        {
            await _s3Client.PutBucketAsync(_options.BucketName);
        }
    }
}