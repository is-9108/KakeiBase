namespace KakeiBase.WebApi.Domain.Entities;

public class User
{
    public Guid Id { get; private set; }
    public string Email { get; private set; } = string.Empty;
    public string PasswordHash { get; private set; } = string.Empty;
    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset UpdatedAt { get; private set; }

    private User() { }

    public static User Create(string email, string passwordHash)
    {
        var now = DateTimeOffset.UtcNow;
        return new User
        {
            Id = Guid.NewGuid(),
            Email = email,
            PasswordHash = passwordHash,
            CreatedAt = now,
            UpdatedAt = now
        };
    }
}
