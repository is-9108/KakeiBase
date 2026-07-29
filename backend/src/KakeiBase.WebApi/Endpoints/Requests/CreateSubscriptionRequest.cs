using FluentValidation;

namespace KakeiBase.WebApi.Endpoints.Requests;

public record CreateSubscriptionRequest(string Name, int Amount);

public class CreateSubscriptionRequestValidator : AbstractValidator<CreateSubscriptionRequest>
{
    public CreateSubscriptionRequestValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(100);
        RuleFor(x => x.Amount).GreaterThan(0);
    }
}
