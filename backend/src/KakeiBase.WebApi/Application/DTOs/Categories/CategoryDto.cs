using KakeiBase.WebApi.Domain.Enums;

namespace KakeiBase.WebApi.Application.DTOs.Categories;

public record CategoryDto(Guid Id, string Name, TransactionType Type, DateTimeOffset CreatedAt);
