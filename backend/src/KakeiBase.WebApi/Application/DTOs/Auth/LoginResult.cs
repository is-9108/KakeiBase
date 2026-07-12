namespace KakeiBase.WebApi.Application.DTOs.Auth;

public record LoginResult(string AccessToken, string RefreshToken, DateTimeOffset AccessTokenExpiresAt);
