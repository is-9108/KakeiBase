using KakeiBase.WebApi.Application.DTOs.Auth;
using KakeiBase.WebApi.Application.Interfaces;
using KakeiBase.WebApi.Domain.Entities;

namespace KakeiBase.WebApi.Application.UseCases.Auth;

/// <summary>メールアドレスとパスワードによるログイン認証を行うユースケース</summary>
public class LoginUseCase(
    IUserRepository userRepository,
    IRefreshTokenRepository refreshTokenRepository,
    IJwtTokenService jwtTokenService,
    IPasswordHasher passwordHasher)
{
    /// <param name="email">ログインするメールアドレス</param>
    /// <param name="password">平文パスワード</param>
    /// <param name="ct">キャンセルトークン</param>
    /// <returns>認証成功時はトークン情報。メールアドレス未存在またはパスワード不一致の場合は null</returns>
    public async Task<LoginResult?> ExecuteAsync(string email, string password, CancellationToken ct = default)
    {
        var user = await userRepository.FindByEmailAsync(email, ct);
        if (user is null)
            return null;

        if (!passwordHasher.Verify(password, user.PasswordHash))
            return null;

        var (accessToken, accessTokenExpiresAt) = jwtTokenService.GenerateAccessToken(user.Id, user.Email);
        var rawRefreshToken = jwtTokenService.GenerateRefreshToken();
        var tokenHash = jwtTokenService.HashRefreshToken(rawRefreshToken);

        var refreshToken = RefreshToken.Create(
            userId: user.Id,
            tokenHash: tokenHash,
            expiresAt: DateTimeOffset.UtcNow.AddDays(7));

        await refreshTokenRepository.AddAsync(refreshToken, ct);
        await refreshTokenRepository.SaveChangesAsync(ct);

        return new LoginResult(accessToken, rawRefreshToken, accessTokenExpiresAt);
    }
}
