namespace KakeiBase.WebApi.Application.Interfaces;

/// <summary>JWT アクセストークンおよびリフレッシュトークンの生成・ハッシュ化を担うサービスインターフェース</summary>
public interface IJwtTokenService
{
    /// <returns>生成されたアクセストークン文字列と有効期限のタプル</returns>
    (string Token, DateTimeOffset ExpiresAt) GenerateAccessToken(Guid userId, string email);

    /// <returns>署名前の生リフレッシュトークン文字列</returns>
    string GenerateRefreshToken();

    /// <summary>生リフレッシュトークンをハッシュ化して返す</summary>
    /// <returns>ハッシュ化済みトークン文字列</returns>
    string HashRefreshToken(string raw);
}
