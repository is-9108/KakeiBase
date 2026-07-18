namespace KakeiBase.WebApi.Domain.Entities;

/// <summary>アプリケーションのユーザーを表すエンティティ</summary>
public class User
{
    public Guid Id { get; private set; }
    public string Email { get; private set; } = string.Empty;
    /// <summary>ハッシュ化済みパスワード。平文は格納しない</summary>
    public string PasswordHash { get; private set; } = string.Empty;
    /// <summary>レコード作成日時（UTC）</summary>
    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset UpdatedAt { get; private set; }

    private User() { }

    /// <summary>新しいユーザーを作成する</summary>
    /// <param name="email">メールアドレス</param>
    /// <param name="passwordHash">ハッシュ化済みパスワード</param>
    /// <returns>作成されたユーザーエンティティ</returns>
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
