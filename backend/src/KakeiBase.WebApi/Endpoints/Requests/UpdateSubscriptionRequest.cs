using FluentValidation;

namespace KakeiBase.WebApi.Endpoints.Requests;

public record UpdateSubscriptionRequest(string Name, int Amount, bool IsActive);

public class UpdateSubscriptionRequestValidator : AbstractValidator<UpdateSubscriptionRequest>
{
    public UpdateSubscriptionRequestValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(100);
        RuleFor(x => x.Amount).GreaterThan(0);
    }
}
