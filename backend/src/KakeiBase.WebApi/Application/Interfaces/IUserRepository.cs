using KakeiBase.WebApi.Domain.Entities;

namespace KakeiBase.WebApi.Application.Interfaces;

public interface IUserRepository
{
    Task<User?> FindByEmailAsync(string email, CancellationToken ct = default);
    Task<User?> FindByIdAsync(Guid id, CancellationToken ct = default);
}
