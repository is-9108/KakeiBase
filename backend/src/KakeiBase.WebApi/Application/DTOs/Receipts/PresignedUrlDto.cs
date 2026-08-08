namespace KakeiBase.WebApi.Application.DTOs.Receipts;

/// <param name="UploadUrl">Presigned PUT URL</param>
/// <param name="S3Key">アップロード先の S3 キー</param>
/// <param name="ExpiresAt">URL の有効期限（ISO 8601 形式）</param>
public record PresignedUrlDto(string UploadUrl, string S3Key, string ExpiresAt);
