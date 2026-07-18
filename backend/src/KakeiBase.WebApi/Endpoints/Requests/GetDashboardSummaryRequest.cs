using FluentValidation;

namespace KakeiBase.WebApi.Endpoints.Requests;

/// <summary>ダッシュボード集計取得のクエリパラメータ</summary>
public record GetDashboardSummaryRequest(int? Year, int? Month);

public class GetDashboardSummaryRequestValidator : AbstractValidator<GetDashboardSummaryRequest>
{
    public GetDashboardSummaryRequestValidator()
    {
        RuleFor(x => x.Year)
            .NotNull().WithMessage("year は必須です")
            .InclusiveBetween(2000, 2100);

        RuleFor(x => x.Month)
            .NotNull().WithMessage("month は必須です")
            .InclusiveBetween(1, 12);
    }
}
