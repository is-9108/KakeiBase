namespace KakeiBase.WebApi.Application.Interfaces;

/// <summary>レシート画像のストレージ操作を抽象化するサービスインターフェース</summary>
public interface IReceiptStorageService
{
    /// <summary>レシート画像アップロード用の Presigned PUT URL を生成する</summary>
    /// <param name="s3Key">アップロード先の S3 キー</param>
    /// <param name="expiresAt">URL の有効期限（UTC）</param>
    /// <returns>Presigned PUT URL</returns>
    Task<string> GeneratePresignedPutUrlAsync(string s3Key, DateTime expiresAt);
}
