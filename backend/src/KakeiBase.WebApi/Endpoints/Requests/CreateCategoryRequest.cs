using FluentValidation;
using KakeiBase.WebApi.Domain.Enums;

namespace KakeiBase.WebApi.Endpoints.Requests;

public record CreateCategoryRequest(string Name, TransactionType Type);

public class CreateCategoryRequestValidator : AbstractValidator<CreateCategoryRequest>
{
    public CreateCategoryRequestValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty()
            .MaximumLength(100);

        RuleFor(x => x.Type)
            .IsInEnum();
    }
}
