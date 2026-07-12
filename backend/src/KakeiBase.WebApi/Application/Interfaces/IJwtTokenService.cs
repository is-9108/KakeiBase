namespace KakeiBase.WebApi.Application.Interfaces;

public interface IJwtTokenService
{
    (string Token, DateTimeOffset ExpiresAt) GenerateAccessToken(Guid userId, string email);
    string GenerateRefreshToken();
    string HashRefreshToken(string raw);
}
