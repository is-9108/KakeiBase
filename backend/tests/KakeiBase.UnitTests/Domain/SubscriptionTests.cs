using FluentAssertions;
using KakeiBase.WebApi.Domain.Entities;
using KakeiBase.WebApi.Domain.Enums;

namespace KakeiBase.UnitTests.Domain;

public class SubscriptionTests
{
    private static Subscription CreateActiveSubscription()
        => Subscription.Create(
            userId: Guid.NewGuid(),
            categoryId: Guid.NewGuid(),
            name: "Netflix",
            amount: 1490m);

    [Fact]
    public void GenerateTransaction_WhenActive_ReturnsExpenseTransaction()
    {
        var subscription = CreateActiveSubscription();
        var date = new DateOnly(2026, 7, 1);

        var transaction = subscription.GenerateTransaction(date);

        transaction.Should().NotBeNull();
        transaction.UserId.Should().Be(subscription.UserId);
        transaction.CategoryId.Should().Be(subscription.CategoryId);
        transaction.SubscriptionId.Should().Be(subscription.Id);
        transaction.Amount.Should().Be((int)subscription.Amount);
        transaction.Type.Should().Be(TransactionType.Expense);
        transaction.Date.Should().Be(date);
        transaction.Memo.Should().Be(subscription.Name);
    }

    [Fact]
    public void GenerateTransaction_WhenInactive_Throws()
    {
        var subscription = CreateActiveSubscription();
        subscription.Deactivate();
        var date = new DateOnly(2026, 7, 1);

        var act = () => subscription.GenerateTransaction(date);

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*無効*");
    }

    [Fact]
    public void Deactivate_SetsIsActiveFalse()
    {
        var subscription = CreateActiveSubscription();

        subscription.Deactivate();

        subscription.IsActive.Should().BeFalse();
    }
}
