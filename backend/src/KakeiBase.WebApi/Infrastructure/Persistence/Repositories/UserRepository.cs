using KakeiBase.WebApi.Application.Interfaces;
using KakeiBase.WebApi.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace KakeiBase.WebApi.Infrastructure.Persistence.Repositories;

public class UserRepository(KakeiBaseDbContext dbContext) : IUserRepository
{
    public Task<User?> FindByEmailAsync(string email, CancellationToken ct = default)
        => dbContext.Users.FirstOrDefaultAsync(u => u.Email == email, ct);

    public Task<User?> FindByIdAsync(Guid id, CancellationToken ct = default)
        => dbContext.Users.FirstOrDefaultAsync(u => u.Id == id, ct);
}
