using FluentAssertions;
using KakeiBase.WebApi.Application.Interfaces;
using KakeiBase.WebApi.Application.UseCases.Subscriptions;
using KakeiBase.WebApi.Domain.Entities;
using NSubstitute;

namespace KakeiBase.UnitTests.Application.Subscriptions;

public class GetSubscriptionsUseCaseTests
{
    private readonly ISubscriptionRepository _subscriptionRepository = Substitute.For<ISubscriptionRepository>();

    private GetSubscriptionsUseCase CreateSut() => new(_subscriptionRepository);

    [Fact]
    public async Task ExecuteAsync_ReturnsAllSubscriptionsForUser()
    {
        var userId = Guid.NewGuid();
        var subscriptions = new List<Subscription>
        {
            Subscription.Create(userId, Guid.NewGuid(), "Netflix", 1490),
            Subscription.Create(userId, Guid.NewGuid(), "Spotify", 980),
        };
        _subscriptionRepository.FindAllByUserIdAsync(userId, null).Returns(subscriptions);

        var sut = CreateSut();
        var result = await sut.ExecuteAsync(userId, null);

        result.Should().HaveCount(2);
        result.Select(s => s.Name).Should().BeEquivalentTo(["Netflix", "Spotify"]);
    }

    [Fact]
    public async Task ExecuteAsync_WithIsActiveFilter_PassesFilterToRepository()
    {
        var userId = Guid.NewGuid();
        _subscriptionRepository.FindAllByUserIdAsync(userId, true).Returns([]);

        var sut = CreateSut();
        await sut.ExecuteAsync(userId, true);

        await _subscriptionRepository.Received(1).FindAllByUserIdAsync(userId, true);
    }

    [Fact]
    public async Task ExecuteAsync_WithNoSubscriptions_ReturnsEmptyList()
    {
        var userId = Guid.NewGuid();
        _subscriptionRepository.FindAllByUserIdAsync(userId, null).Returns([]);

        var sut = CreateSut();
        var result = await sut.ExecuteAsync(userId, null);

        result.Should().BeEmpty();
    }
}
