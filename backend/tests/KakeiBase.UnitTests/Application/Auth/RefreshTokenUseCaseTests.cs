using FluentAssertions;
using KakeiBase.WebApi.Application.Interfaces;
using KakeiBase.WebApi.Application.UseCases.Auth;
using KakeiBase.WebApi.Domain.Entities;
using NSubstitute;

namespace KakeiBase.UnitTests.Application.Auth;

public class RefreshTokenUseCaseTests
{
    private readonly IRefreshTokenRepository _refreshTokenRepository = Substitute.For<IRefreshTokenRepository>();
    private readonly IUserRepository _userRepository = Substitute.For<IUserRepository>();
    private readonly IJwtTokenService _jwtTokenService = Substitute.For<IJwtTokenService>();

    private RefreshTokenUseCase CreateSut() =>
        new(_refreshTokenRepository, _userRepository, _jwtTokenService);

    private static RefreshToken CreateActiveToken(Guid userId)
        => RefreshToken.Create(userId, "token_hash", DateTimeOffset.UtcNow.AddDays(7));

    [Fact]
    public async Task ExecuteAsync_WithValidToken_ReturnsNewLoginResultAndRevokesOldToken()
    {
        var userId = Guid.NewGuid();
        var user = User.Create("test@example.com", "hashed");
        var existingToken = CreateActiveToken(userId);

        _jwtTokenService.HashRefreshToken("raw_token").Returns("token_hash");
        _refreshTokenRepository.FindByTokenHashAsync("token_hash").Returns(existingToken);
        _userRepository.FindByIdAsync(existingToken.UserId).Returns(user);
        _jwtTokenService.GenerateAccessToken(user.Id, user.Email)
            .Returns(("new_access_token", DateTimeOffset.UtcNow.AddMinutes(15)));
        _jwtTokenService.GenerateRefreshToken().Returns("new_raw_refresh");
        _jwtTokenService.HashRefreshToken("new_raw_refresh").Returns("new_hash");

        var sut = CreateSut();
        var result = await sut.ExecuteAsync("raw_token");

        result.Should().NotBeNull();
        result!.AccessToken.Should().Be("new_access_token");
        result.RefreshToken.Should().Be("new_raw_refresh");
        existingToken.IsRevoked.Should().BeTrue();
    }

    [Fact]
    public async Task ExecuteAsync_WithNonExistentToken_ReturnsNull()
    {
        _jwtTokenService.HashRefreshToken("unknown_token").Returns("unknown_hash");
        _refreshTokenRepository.FindByTokenHashAsync("unknown_hash").Returns((RefreshToken?)null);

        var sut = CreateSut();
        var result = await sut.ExecuteAsync("unknown_token");

        result.Should().BeNull();
    }

    [Fact]
    public async Task ExecuteAsync_WithExpiredToken_ReturnsNull()
    {
        var userId = Guid.NewGuid();
        var expiredToken = RefreshToken.Create(userId, "expired_hash", DateTimeOffset.UtcNow.AddDays(-1));

        _jwtTokenService.HashRefreshToken("raw_token").Returns("expired_hash");
        _refreshTokenRepository.FindByTokenHashAsync("expired_hash").Returns(expiredToken);

        var sut = CreateSut();
        var result = await sut.ExecuteAsync("raw_token");

        result.Should().BeNull();
    }

    [Fact]
    public async Task ExecuteAsync_WithRevokedToken_ReturnsNull()
    {
        var userId = Guid.NewGuid();
        var revokedToken = CreateActiveToken(userId);
        revokedToken.Revoke();

        _jwtTokenService.HashRefreshToken("raw_token").Returns("token_hash");
        _refreshTokenRepository.FindByTokenHashAsync("token_hash").Returns(revokedToken);

        var sut = CreateSut();
        var result = await sut.ExecuteAsync("raw_token");

        result.Should().BeNull();
    }
}
