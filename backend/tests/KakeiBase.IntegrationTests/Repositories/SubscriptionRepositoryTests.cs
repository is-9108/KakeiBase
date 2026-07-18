using KakeiBase.WebApi.Domain.Entities;
using KakeiBase.WebApi.Domain.Enums;
using KakeiBase.WebApi.Infrastructure.Persistence;
using KakeiBase.WebApi.Infrastructure.Persistence.Repositories;
using Microsoft.EntityFrameworkCore;
using Testcontainers.PostgreSql;

namespace KakeiBase.IntegrationTests.Repositories;

public class SubscriptionRepositoryTests : IAsyncLifetime
{
    private readonly PostgreSqlContainer _postgres = new PostgreSqlBuilder()
        .WithImage("postgres:16-alpine")
        .Build();

    private KakeiBaseDbContext _dbContext = null!;
    private SubscriptionRepository _sut = null!;

    public async Task InitializeAsync()
    {
        await _postgres.StartAsync();

        var options = new DbContextOptionsBuilder<KakeiBaseDbContext>()
            .UseNpgsql(_postgres.GetConnectionString())
            .Options;

        _dbContext = new KakeiBaseDbContext(options);
        await _dbContext.Database.MigrateAsync();
        _sut = new SubscriptionRepository(_dbContext);
    }

    public async Task DisposeAsync()
    {
        await _dbContext.DisposeAsync();
        await _postgres.DisposeAsync();
    }

    private async Task<(User user, Category category)> CreateUserAndCategoryAsync(string email = "test@example.com")
    {
        var user = User.Create(email, "hashed_password");
        await _dbContext.Users.AddAsync(user);
        var category = Category.Create(user.Id, "エンタメ", TransactionType.Expense);
        await _dbContext.Categories.AddAsync(category);
        await _dbContext.SaveChangesAsync();
        return (user, category);
    }

    [Fact]
    public async Task AddAsync_AndFindByIdAsync_ReturnsCreatedSubscription()
    {
        var (user, category) = await CreateUserAndCategoryAsync();
        var subscription = Subscription.Create(user.Id, category.Id, "Netflix", 1490);

        await _sut.AddAsync(subscription);
        await _sut.SaveChangesAsync();

        var found = await _sut.FindByIdAsync(subscription.Id);

        Assert.NotNull(found);
        Assert.Equal("Netflix", found.Name);
        Assert.Equal(1490, found.Amount);
        Assert.True(found.IsActive);
        Assert.Equal(user.Id, found.UserId);
        Assert.Equal(category.Id, found.CategoryId);
    }

    [Fact]
    public async Task FindAllByUserIdAsync_ReturnsOnlyOwnSubscriptions()
    {
        var (user, category) = await CreateUserAndCategoryAsync("user1@example.com");
        var (otherUser, otherCategory) = await CreateUserAndCategoryAsync("user2@example.com");

        var s1 = Subscription.Create(user.Id, category.Id, "Netflix", 1490);
        var s2 = Subscription.Create(user.Id, category.Id, "Spotify", 980);
        var s3 = Subscription.Create(otherUser.Id, otherCategory.Id, "Amazon", 600);

        await _sut.AddAsync(s1);
        await _sut.AddAsync(s2);
        await _sut.AddAsync(s3);
        await _sut.SaveChangesAsync();

        var results = await _sut.FindAllByUserIdAsync(user.Id);

        Assert.Equal(2, results.Count);
        Assert.All(results, s => Assert.Equal(user.Id, s.UserId));
    }

    [Fact]
    public async Task FindAllByUserIdAsync_WithIsActiveFilter_ReturnsOnlyMatchingSubscriptions()
    {
        var (user, category) = await CreateUserAndCategoryAsync();

        var active = Subscription.Create(user.Id, category.Id, "Netflix", 1490);
        var inactive = Subscription.Create(user.Id, category.Id, "Spotify", 980);
        inactive.Deactivate();

        await _sut.AddAsync(active);
        await _sut.AddAsync(inactive);
        await _sut.SaveChangesAsync();

        var activeResults = await _sut.FindAllByUserIdAsync(user.Id, isActive: true);
        var inactiveResults = await _sut.FindAllByUserIdAsync(user.Id, isActive: false);

        Assert.Single(activeResults);
        Assert.Equal(active.Id, activeResults[0].Id);

        Assert.Single(inactiveResults);
        Assert.Equal(inactive.Id, inactiveResults[0].Id);
    }

    [Fact]
    public async Task DeleteAsync_RemovesSubscription()
    {
        var (user, category) = await CreateUserAndCategoryAsync();
        var subscription = Subscription.Create(user.Id, category.Id, "Netflix", 1490);

        await _sut.AddAsync(subscription);
        await _sut.SaveChangesAsync();

        await _sut.DeleteAsync(subscription);
        await _sut.SaveChangesAsync();

        var found = await _sut.FindByIdAsync(subscription.Id);
        Assert.Null(found);
    }
}
