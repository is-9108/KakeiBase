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
        var systemCategory = Category.CreateSystem(userId, "サブスク", TransactionType.Expense);
        _categoryRepository.FindSystemSubscriptionCategoryByUserIdAsync(userId).Returns(systemCategory);

        var sut = CreateSut();
        var result = await sut.ExecuteAsync(userId, "Netflix", 1490);

        result.Should().NotBeNull();
        result!.CategoryId.Should().Be(systemCategory.Id);
        result.Name.Should().Be("Netflix");
        result.Amount.Should().Be(1490);
        result.IsActive.Should().BeTrue();
        await _subscriptionRepository.Received(1).AddAsync(Arg.Any<Subscription>());
        await _subscriptionRepository.Received(1).SaveChangesAsync();
    }

    [Fact]
    public async Task ExecuteAsync_SystemCategoryNotFound_ReturnsNull()
    {
        var userId = Guid.NewGuid();
        _categoryRepository.FindSystemSubscriptionCategoryByUserIdAsync(userId).Returns((Category?)null);

        var sut = CreateSut();
        var result = await sut.ExecuteAsync(userId, "Netflix", 1490);

        result.Should().BeNull();
        await _subscriptionRepository.DidNotReceive().AddAsync(Arg.Any<Subscription>());
    }
}
