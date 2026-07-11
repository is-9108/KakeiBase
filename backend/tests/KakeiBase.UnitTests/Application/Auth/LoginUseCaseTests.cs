using FluentAssertions;
using KakeiBase.WebApi.Application.Interfaces;
using KakeiBase.WebApi.Application.UseCases.Auth;
using KakeiBase.WebApi.Domain.Entities;
using NSubstitute;

namespace KakeiBase.UnitTests.Application.Auth;

public class LoginUseCaseTests
{
    private readonly IUserRepository _userRepository = Substitute.For<IUserRepository>();
    private readonly IRefreshTokenRepository _refreshTokenRepository = Substitute.For<IRefreshTokenRepository>();
    private readonly IJwtTokenService _jwtTokenService = Substitute.For<IJwtTokenService>();
    private readonly IPasswordHasher _passwordHasher = Substitute.For<IPasswordHasher>();

    private LoginUseCase CreateSut() =>
        new(_userRepository, _refreshTokenRepository, _jwtTokenService, _passwordHasher);

    [Fact]
    public async Task ExecuteAsync_WithValidCredentials_ReturnsLoginResult()
    {
        var user = User.Create("test@example.com", "hashed_password");
        _userRepository.FindByEmailAsync("test@example.com").Returns(user);
        _passwordHasher.Verify("password123", "hashed_password").Returns(true);
        _jwtTokenService.GenerateAccessToken(user.Id, user.Email)
            .Returns(("access_token_value", DateTimeOffset.UtcNow.AddMinutes(15)));
        _jwtTokenService.GenerateRefreshToken().Returns("raw_refresh_token");
        _jwtTokenService.HashRefreshToken("raw_refresh_token").Returns("hashed_refresh_token");

        var sut = CreateSut();
        var result = await sut.ExecuteAsync("test@example.com", "password123");

        result.Should().NotBeNull();
        result!.AccessToken.Should().Be("access_token_value");
        result.RefreshToken.Should().Be("raw_refresh_token");
    }

    [Fact]
    public async Task ExecuteAsync_WithNonExistentEmail_ReturnsNull()
    {
        _userRepository.FindByEmailAsync("notfound@example.com").Returns((User?)null);

        var sut = CreateSut();
        var result = await sut.ExecuteAsync("notfound@example.com", "password123");

        result.Should().BeNull();
    }

    [Fact]
    public async Task ExecuteAsync_WithWrongPassword_ReturnsNull()
    {
        var user = User.Create("test@example.com", "hashed_password");
        _userRepository.FindByEmailAsync("test@example.com").Returns(user);
        _passwordHasher.Verify("wrongpassword", "hashed_password").Returns(false);

        var sut = CreateSut();
        var result = await sut.ExecuteAsync("test@example.com", "wrongpassword");

        result.Should().BeNull();
    }
}
