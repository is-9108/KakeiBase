using KakeiBase.WebApi.Application.Interfaces;

namespace KakeiBase.WebApi.Application.UseCases.Auth;

/// <summary>リフレッシュトークンを失効させてログアウトするユースケース</summary>
public class LogoutUseCase(
    IRefreshTokenRepository refreshTokenRepository,
    IJwtTokenService jwtTokenService)
{
    /// <param name="rawToken">ハッシュ前の生リフレッシュトークン文字列</param>
    /// <param name="ct">キャンセルトークン</param>
    public async Task ExecuteAsync(string rawToken, CancellationToken ct = default)
    {
        var tokenHash = jwtTokenService.HashRefreshToken(rawToken);
        var token = await refreshTokenRepository.FindByTokenHashAsync(tokenHash, ct);

        if (token is not null && !token.IsRevoked)
        {
            token.Revoke();
            await refreshTokenRepository.SaveChangesAsync(ct);
        }
    }
}
