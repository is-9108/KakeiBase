using KakeiBase.WebApi.Application.DTOs.Receipts;
using KakeiBase.WebApi.Application.Interfaces;

namespace KakeiBase.WebApi.Application.UseCases.Receipts;

/// <summary>レシート画像アップロード用 Presigned URL を生成するユースケース</summary>
public class GeneratePresignedUrlUseCase(IReceiptStorageService receiptStorageService)
{
    private static readonly TimeSpan PresignedUrlExpiry = TimeSpan.FromMinutes(5);
    private static readonly TimeZoneInfo Jst = TimeZoneInfo.FindSystemTimeZoneById("Tokyo Standard Time");

    /// <param name="userId">リクエストしたユーザーの ID</param>
    /// <param name="ct">キャンセルトークン</param>
    /// <returns>Presigned PUT URL・S3 キー・有効期限を含む DTO</returns>
    public async Task<PresignedUrlDto> ExecuteAsync(Guid userId, CancellationToken ct = default)
    {
        var jstNow = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, Jst);
        var s3Key = $"receipts/{userId}/{jstNow:yyyy}/{jstNow:MM}/{jstNow:yyyyMMdd}_{jstNow:HHmmss}.{jstNow:fff}.jpg";
        var expiresAt = DateTime.UtcNow.Add(PresignedUrlExpiry);
        var uploadUrl = await receiptStorageService.GeneratePresignedPutUrlAsync(s3Key, expiresAt);
        return new PresignedUrlDto(uploadUrl, s3Key, expiresAt.ToString("o"));
    }
}
