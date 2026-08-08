using Amazon.S3;
using Amazon.S3.Model;
using KakeiBase.WebApi.Application.Interfaces;

namespace KakeiBase.WebApi.Infrastructure.Storage;

/// <summary>Amazon S3 を使ったレシート画像ストレージサービス</summary>
public class S3ReceiptStorageService(IAmazonS3 s3Client, IConfiguration configuration) : IReceiptStorageService
{
    private readonly string _bucketName = configuration["Aws:S3:ReceiptBucket"] ?? string.Empty;

    /// <inheritdoc />
    public async Task<string> GeneratePresignedPutUrlAsync(string s3Key, DateTime expiresAt)
    {
        if (string.IsNullOrEmpty(_bucketName))
            throw new InvalidOperationException("S3 receipt bucket name is not configured (Aws:S3:ReceiptBucket).");

        var request = new GetPreSignedUrlRequest
        {
            BucketName = _bucketName,
            Key = s3Key,
            Verb = HttpVerb.PUT,
            Expires = expiresAt
        };
        return await s3Client.GetPreSignedURLAsync(request);
    }
}
