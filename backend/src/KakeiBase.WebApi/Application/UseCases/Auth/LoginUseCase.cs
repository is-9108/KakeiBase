using KakeiBase.WebApi.Application.DTOs.Auth;
using KakeiBase.WebApi.Application.Interfaces;
using KakeiBase.WebApi.Domain.Entities;

namespace KakeiBase.WebApi.Application.UseCases.Auth;

public class LoginUseCase(
    IUserRepository userRepository,
    IRefreshTokenRepository refreshTokenRepository,
    IJwtTokenService jwtTokenService,
    IPasswordHasher passwordHasher)
{
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
