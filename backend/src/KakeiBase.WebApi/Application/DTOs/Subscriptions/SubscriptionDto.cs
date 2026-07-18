namespace KakeiBase.WebApi.Application.DTOs.Subscriptions;

public record SubscriptionDto(
    Guid Id,
    Guid CategoryId,
    string Name,
    int Amount,
    bool IsActive,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt);
