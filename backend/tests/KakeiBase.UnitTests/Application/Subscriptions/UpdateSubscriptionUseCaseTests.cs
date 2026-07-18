using FluentAssertions;
using KakeiBase.WebApi.Application.Interfaces;
using KakeiBase.WebApi.Application.UseCases.Subscriptions;
using KakeiBase.WebApi.Domain.Entities;
using KakeiBase.WebApi.Domain.Enums;
using NSubstitute;

namespace KakeiBase.UnitTests.Application.Subscriptions;

public class UpdateSubscriptionUseCaseTests
{
    private readonly ISubscriptionRepository _subscriptionRepository = Substitute.For<ISubscriptionRepository>();
    private readonly ICategoryRepository _categoryRepository = Substitute.For<ICategoryRepository>();

    private UpdateSubscriptionUseCase CreateSut() => new(_subscriptionRepository, _categoryRepository);

    private static Subscription CreateSubscription(Guid userId)
        => Subscription.Create(userId, Guid.NewGuid(), "Netflix", 1490);

    [Fact]
    public async Task ExecuteAsync_WithValidUpdate_ReturnsUpdatedDto()
    {
        var userId = Guid.NewGuid();
        var subscription = CreateSubscription(userId);
        var newCategoryId = Guid.NewGuid();
        var category = Category.Create(userId, "サブスク", TransactionType.Expense);

        _subscriptionRepository.FindByIdAsync(subscription.Id).Returns(subscription);
        _categoryRepository.FindByIdAsync(newCategoryId).Returns(category);

        var sut = CreateSut();
        var result = await sut.ExecuteAsync(userId, subscription.Id, newCategoryId, "Disney+", 990, false);

        result.IsNotFound.Should().BeFalse();
        result.Subscription.Should().NotBeNull();
        result.Subscription!.Name.Should().Be("Disney+");
        result.Subscription.Amount.Should().Be(990);
        result.Subscription.IsActive.Should().BeFalse();
        result.Subscription.CategoryId.Should().Be(newCategoryId);
        await _subscriptionRepository.Received(1).SaveChangesAsync();
    }

    [Fact]
    public async Task ExecuteAsync_WithNonExistentId_ReturnsNotFound()
    {
        var userId = Guid.NewGuid();
        var subscriptionId = Guid.NewGuid();

        _subscriptionRepository.FindByIdAsync(subscriptionId).Returns((Subscription?)null);

        var sut = CreateSut();
        var result = await sut.ExecuteAsync(userId, subscriptionId, Guid.NewGuid(), "Netflix", 1490, true);

        result.IsNotFound.Should().BeTrue();
        result.Subscription.Should().BeNull();
        await _subscriptionRepository.DidNotReceive().SaveChangesAsync();
    }

    [Fact]
    public async Task ExecuteAsync_WithOtherUsersSubscription_ReturnsNotFound()
    {
        var userId = Guid.NewGuid();
        var otherUserId = Guid.NewGuid();
        var subscription = CreateSubscription(otherUserId);

        _subscriptionRepository.FindByIdAsync(subscription.Id).Returns(subscription);

        var sut = CreateSut();
        var result = await sut.ExecuteAsync(userId, subscription.Id, Guid.NewGuid(), "Netflix", 1490, true);

        result.IsNotFound.Should().BeTrue();
        result.Subscription.Should().BeNull();
        await _subscriptionRepository.DidNotReceive().SaveChangesAsync();
    }

    [Fact]
    public async Task ExecuteAsync_CategoryNotFound_ReturnsNotFound()
    {
        var userId = Guid.NewGuid();
        var subscription = CreateSubscription(userId);
        var categoryId = Guid.NewGuid();

        _subscriptionRepository.FindByIdAsync(subscription.Id).Returns(subscription);
        _categoryRepository.FindByIdAsync(categoryId).Returns((Category?)null);

        var sut = CreateSut();
        var result = await sut.ExecuteAsync(userId, subscription.Id, categoryId, "Netflix", 1490, true);

        result.IsNotFound.Should().BeTrue();
        result.Subscription.Should().BeNull();
        await _subscriptionRepository.DidNotReceive().SaveChangesAsync();
    }

    [Fact]
    public async Task ExecuteAsync_CategoryOwnedByOtherUser_ReturnsNotFound()
    {
        var userId = Guid.NewGuid();
        var otherUserId = Guid.NewGuid();
        var subscription = CreateSubscription(userId);
        var categoryId = Guid.NewGuid();
        var category = Category.Create(otherUserId, "他ユーザーのカテゴリ", TransactionType.Expense);

        _subscriptionRepository.FindByIdAsync(subscription.Id).Returns(subscription);
        _categoryRepository.FindByIdAsync(categoryId).Returns(category);

        var sut = CreateSut();
        var result = await sut.ExecuteAsync(userId, subscription.Id, categoryId, "Netflix", 1490, true);

        result.IsNotFound.Should().BeTrue();
        result.Subscription.Should().BeNull();
        await _subscriptionRepository.DidNotReceive().SaveChangesAsync();
    }
}
