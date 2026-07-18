namespace KakeiBase.WebApi.Domain.Entities;

/// <summary>リフレッシュトークンを表すエンティティ</summary>
public class RefreshToken
{
    /// <summary>ID</summary>
    public Guid Id { get; private set; }
    /// <summary>ユーザーID</summary>
    public Guid UserId { get; private set; }
    /// <summary>ハッシュ化済みトークン文字列。平文は格納しない</summary>
    public string TokenHash { get; private set; } = string.Empty;
    /// <summary>トークンの有効期限（UTC）</summary>
    public DateTimeOffset ExpiresAt { get; private set; }
    /// <summary>レコード作成日時（UTC）</summary>
    public DateTimeOffset CreatedAt { get; private set; }
    /// <summary>明示的に失効させたかどうかを示すフラグ</summary>
    public bool IsRevoked { get; private set; }

    /// <summary>有効期限が切れているかどうか</summary>
    public bool IsExpired => DateTimeOffset.UtcNow >= ExpiresAt;
    /// <summary>失効しておらず有効期限内であるかどうか</summary>
    public bool IsActive => !IsRevoked && !IsExpired;

    private RefreshToken() { }

    /// <summary>新しいリフレッシュトークンを作成する</summary>
    /// <param name="userId">トークンが紐づくユーザーのID</param>
    /// <param name="tokenHash">ハッシュ化済みトークン文字列</param>
    /// <param name="expiresAt">有効期限（UTC）</param>
    /// <returns>作成されたリフレッシュトークンエンティティ</returns>
    public static RefreshToken Create(Guid userId, string tokenHash, DateTimeOffset expiresAt)
    {
        return new RefreshToken
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            TokenHash = tokenHash,
            ExpiresAt = expiresAt,
            CreatedAt = DateTimeOffset.UtcNow,
            IsRevoked = false
        };
    }

    /// <summary>トークンを失効させる</summary>
    public void Revoke()
    {
        IsRevoked = true;
    }
}
