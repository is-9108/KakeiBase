using FluentValidation;

namespace KakeiBase.WebApi.Endpoints.Requests;

public record UpdateTransactionRequest(
    Guid CategoryId,
    int Amount,
    DateOnly Date,
    string? Memo,
    string? ReceiptS3Key);

public class UpdateTransactionRequestValidator : AbstractValidator<UpdateTransactionRequest>
{
    public UpdateTransactionRequestValidator()
    {
        RuleFor(x => x.CategoryId)
            .NotEmpty();

        RuleFor(x => x.Amount)
            .GreaterThan(0);

        RuleFor(x => x.Date)
            .NotEmpty();

        RuleFor(x => x.Memo)
            .MaximumLength(500)
            .When(x => x.Memo is not null);
    }
}
