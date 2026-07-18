using FluentAssertions;
using KakeiBase.WebApi.Application.Interfaces;
using KakeiBase.WebApi.Application.UseCases.Subscriptions;
using KakeiBase.WebApi.Domain.Entities;
using NSubstitute;

namespace KakeiBase.UnitTests.Application.Subscriptions;

public class CreateSubscriptionUseCaseTests
{
    private readonly ISubscriptionRepository _subscriptionRepository = Substitute.For<ISubscriptionRepository>();

    private CreateSubscriptionUseCase CreateSut() => new(_subscriptionRepository);

    [Fact]
    public async Task ExecuteAsync_WithValidInput_ReturnsSubscriptionDto()
    {
        var userId = Guid.NewGuid();
        var categoryId = Guid.NewGuid();

        var sut = CreateSut();
        var result = await sut.ExecuteAsync(userId, categoryId, "Netflix", 1490);

        result.Should().NotBeNull();
        result.CategoryId.Should().Be(categoryId);
        result.Name.Should().Be("Netflix");
        result.Amount.Should().Be(1490);
        result.IsActive.Should().BeTrue();
        await _subscriptionRepository.Received(1).AddAsync(Arg.Any<Subscription>());
        await _subscriptionRepository.Received(1).SaveChangesAsync();
    }
}
