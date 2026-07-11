using KakeiBase.WebApi.Application.DTOs.Auth;
using KakeiBase.WebApi.Application.Interfaces;
using KakeiBase.WebApi.Domain.Entities;

namespace KakeiBase.WebApi.Application.UseCases.Auth;

public class RefreshTokenUseCase(
    IRefreshTokenRepository refreshTokenRepository,
    IUserRepository userRepository,
    IJwtTokenService jwtTokenService)
{
    public async Task<LoginResult?> ExecuteAsync(string rawToken, CancellationToken ct = default)
    {
        var tokenHash = jwtTokenService.HashRefreshToken(rawToken);
        var existingToken = await refreshTokenRepository.FindByTokenHashAsync(tokenHash, ct);

        if (existingToken is null || !existingToken.IsActive)
            return null;

        var user = await userRepository.FindByIdAsync(existingToken.UserId, ct);
        if (user is null)
            return null;

        existingToken.Revoke();

        var (accessToken, accessTokenExpiresAt) = jwtTokenService.GenerateAccessToken(user.Id, user.Email);
        var newRawToken = jwtTokenService.GenerateRefreshToken();
        var newTokenHash = jwtTokenService.HashRefreshToken(newRawToken);

        var newRefreshToken = RefreshToken.Create(
            userId: user.Id,
            tokenHash: newTokenHash,
            expiresAt: DateTimeOffset.UtcNow.AddDays(7));

        await refreshTokenRepository.AddAsync(newRefreshToken, ct);
        await refreshTokenRepository.SaveChangesAsync(ct);

        return new LoginResult(accessToken, newRawToken, accessTokenExpiresAt);
    }
}
