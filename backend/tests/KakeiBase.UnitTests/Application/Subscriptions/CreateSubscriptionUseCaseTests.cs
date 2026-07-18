using FluentAssertions;
using KakeiBase.WebApi.Application.Interfaces;
using KakeiBase.WebApi.Application.UseCases.Subscriptions;
using KakeiBase.WebApi.Domain.Entities;
using KakeiBase.WebApi.Domain.Enums;
using NSubstitute;

namespace KakeiBase.UnitTests.Application.Subscriptions;

public class CreateSubscriptionUseCaseTests
{
    private readonly ISubscriptionRepository _subscriptionRepository = Substitute.For<ISubscriptionRepository>();
    private readonly ICategoryRepository _categoryRepository = Substitute.For<ICategoryRepository>();

    private CreateSubscriptionUseCase CreateSut() => new(_subscriptionRepository, _categoryRepository);

    [Fact]
    public async Task ExecuteAsync_WithValidInput_ReturnsSubscriptionDto()
    {
        var userId = Guid.NewGuid();
        var categoryId = Guid.NewGuid();
        var category = Category.Create(userId, "サブスク", TransactionType.Expense);
        _categoryRepository.FindByIdAsync(categoryId).Returns(category);

        var sut = CreateSut();
        var result = await sut.ExecuteAsync(userId, categoryId, "Netflix", 1490);

        result.Should().NotBeNull();
        result!.CategoryId.Should().Be(categoryId);
        result.Name.Should().Be("Netflix");
        result.Amount.Should().Be(1490);
        result.IsActive.Should().BeTrue();
        await _subscriptionRepository.Received(1).AddAsync(Arg.Any<Subscription>());
        await _subscriptionRepository.Received(1).SaveChangesAsync();
    }

    [Fact]
    public async Task ExecuteAsync_CategoryNotFound_ReturnsNull()
    {
        var userId = Guid.NewGuid();
        var categoryId = Guid.NewGuid();
        _categoryRepository.FindByIdAsync(categoryId).Returns((Category?)null);

        var sut = CreateSut();
        var result = await sut.ExecuteAsync(userId, categoryId, "Netflix", 1490);

        result.Should().BeNull();
        await _subscriptionRepository.DidNotReceive().AddAsync(Arg.Any<Subscription>());
    }

    [Fact]
    public async Task ExecuteAsync_CategoryOwnedByOtherUser_ReturnsNull()
    {
        var userId = Guid.NewGuid();
        var otherUserId = Guid.NewGuid();
        var categoryId = Guid.NewGuid();
        var category = Category.Create(otherUserId, "他ユーザーのカテゴリ", TransactionType.Expense);
        _categoryRepository.FindByIdAsync(categoryId).Returns(category);

        var sut = CreateSut();
        var result = await sut.ExecuteAsync(userId, categoryId, "Netflix", 1490);

        result.Should().BeNull();
        await _subscriptionRepository.DidNotReceive().AddAsync(Arg.Any<Subscription>());
    }
}
