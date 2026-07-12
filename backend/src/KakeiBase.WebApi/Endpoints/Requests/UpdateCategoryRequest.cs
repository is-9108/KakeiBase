using FluentValidation;
using KakeiBase.WebApi.Domain.Enums;

namespace KakeiBase.WebApi.Endpoints.Requests;

public record UpdateCategoryRequest(string Name, TransactionType Type);

public class UpdateCategoryRequestValidator : AbstractValidator<UpdateCategoryRequest>
{
    public UpdateCategoryRequestValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty()
            .MaximumLength(100);

        RuleFor(x => x.Type)
            .IsInEnum();
    }
}
