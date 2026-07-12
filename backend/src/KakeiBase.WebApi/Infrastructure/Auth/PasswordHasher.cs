using KakeiBase.WebApi.Application.Interfaces;

namespace KakeiBase.WebApi.Infrastructure.Auth;

public class PasswordHasher : IPasswordHasher
{
    public bool Verify(string password, string hash)
        => BCrypt.Net.BCrypt.Verify(password, hash);
}
