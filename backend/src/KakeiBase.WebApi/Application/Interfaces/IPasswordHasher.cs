namespace KakeiBase.WebApi.Application.Interfaces;

/// <summary>パスワードのハッシュ化と検証を担うサービスインターフェース</summary>
public interface IPasswordHasher
{
    /// <summary>パスワードとハッシュを検証する</summary>
    /// <returns>パスワードがハッシュと一致する場合は true</returns>
    bool Verify(string password, string hash);
}
