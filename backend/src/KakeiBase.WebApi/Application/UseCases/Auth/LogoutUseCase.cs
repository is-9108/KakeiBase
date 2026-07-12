using KakeiBase.WebApi.Application.Interfaces;

namespace KakeiBase.WebApi.Application.UseCases.Auth;

public class LogoutUseCase(
    IRefreshTokenRepository refreshTokenRepository,
    IJwtTokenService jwtTokenService)
{
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
