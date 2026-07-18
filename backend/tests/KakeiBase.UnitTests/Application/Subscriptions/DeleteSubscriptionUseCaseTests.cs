using FluentAssertions;
using KakeiBase.WebApi.Application.Interfaces;
using KakeiBase.WebApi.Application.UseCases.Subscriptions;
using KakeiBase.WebApi.Domain.Entities;
using NSubstitute;

namespace KakeiBase.UnitTests.Application.Subscriptions;

public class DeleteSubscriptionUseCaseTests
{
    private readonly ISubscriptionRepository _subscriptionRepository = Substitute.For<ISubscriptionRepository>();

    private DeleteSubscriptionUseCase CreateSut() => new(_subscriptionRepository);

    private static Subscription CreateSubscription(Guid userId)
        => Subscription.Create(userId, Guid.NewGuid(), "Netflix", 1490);

    [Fact]
    public async Task ExecuteAsync_WithExistingSubscription_DeletesAndReturnsTrue()
    {
        var userId = Guid.NewGuid();
        var subscription = CreateSubscription(userId);

        _subscriptionRepository.FindByIdAsync(subscription.Id).Returns(subscription);

        var sut = CreateSut();
        var result = await sut.ExecuteAsync(userId, subscription.Id);

        result.Should().BeTrue();
        await _subscriptionRepository.Received(1).DeleteAsync(subscription);
        await _subscriptionRepository.Received(1).SaveChangesAsync();
    }

    [Fact]
    public async Task ExecuteAsync_WithNonExistentId_ReturnsFalse()
    {
        var userId = Guid.NewGuid();
        var subscriptionId = Guid.NewGuid();

        _subscriptionRepository.FindByIdAsync(subscriptionId).Returns((Subscription?)null);

        var sut = CreateSut();
        var result = await sut.ExecuteAsync(userId, subscriptionId);

        result.Should().BeFalse();
        await _subscriptionRepository.DidNotReceive().DeleteAsync(Arg.Any<Subscription>());
        await _subscriptionRepository.DidNotReceive().SaveChangesAsync();
    }

    [Fact]
    public async Task ExecuteAsync_WithOtherUsersSubscription_ReturnsFalse()
    {
        var userId = Guid.NewGuid();
        var otherUserId = Guid.NewGuid();
        var subscription = CreateSubscription(otherUserId);

        _subscriptionRepository.FindByIdAsync(subscription.Id).Returns(subscription);

        var sut = CreateSut();
        var result = await sut.ExecuteAsync(userId, subscription.Id);

        result.Should().BeFalse();
        await _subscriptionRepository.DidNotReceive().DeleteAsync(Arg.Any<Subscription>());
        await _subscriptionRepository.DidNotReceive().SaveChangesAsync();
    }
}
