using KakeiBase.WebApi.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace KakeiBase.WebApi.Infrastructure.Persistence;

public class KakeiBaseDbContext(DbContextOptions<KakeiBaseDbContext> options) : DbContext(options)
{
    public DbSet<User> Users => Set<User>();
    public DbSet<Category> Categories => Set<Category>();
    public DbSet<Transaction> Transactions => Set<Transaction>();
    public DbSet<Subscription> Subscriptions => Set<Subscription>();
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
        => modelBuilder.ApplyConfigurationsFromAssembly(typeof(KakeiBaseDbContext).Assembly);
}
