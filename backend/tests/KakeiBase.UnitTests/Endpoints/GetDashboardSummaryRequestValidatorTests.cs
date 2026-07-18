using FluentAssertions;
using FluentValidation.TestHelper;
using KakeiBase.WebApi.Endpoints.Requests;

namespace KakeiBase.UnitTests.Endpoints;

public class GetDashboardSummaryRequestValidatorTests
{
    private readonly GetDashboardSummaryRequestValidator _validator = new();

    [Fact]
    public void Validate_WithValidParams_IsValid()
    {
        var result = _validator.TestValidate(new GetDashboardSummaryRequest(2026, 7));
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Validate_YearNull_IsInvalid()
    {
        var result = _validator.TestValidate(new GetDashboardSummaryRequest(null, 7));
        result.ShouldHaveValidationErrorFor(x => x.Year);
    }

    [Fact]
    public void Validate_YearTooSmall_IsInvalid()
    {
        var result = _validator.TestValidate(new GetDashboardSummaryRequest(1999, 7));
        result.ShouldHaveValidationErrorFor(x => x.Year);
    }

    [Fact]
    public void Validate_YearAtLowerBound_IsValid()
    {
        var result = _validator.TestValidate(new GetDashboardSummaryRequest(2000, 7));
        result.ShouldNotHaveValidationErrorFor(x => x.Year);
    }

    [Fact]
    public void Validate_YearAtUpperBound_IsValid()
    {
        var result = _validator.TestValidate(new GetDashboardSummaryRequest(2100, 7));
        result.ShouldNotHaveValidationErrorFor(x => x.Year);
    }

    [Fact]
    public void Validate_YearTooLarge_IsInvalid()
    {
        var result = _validator.TestValidate(new GetDashboardSummaryRequest(2101, 7));
        result.ShouldHaveValidationErrorFor(x => x.Year);
    }

    [Fact]
    public void Validate_MonthNull_IsInvalid()
    {
        var result = _validator.TestValidate(new GetDashboardSummaryRequest(2026, null));
        result.ShouldHaveValidationErrorFor(x => x.Month);
    }

    [Fact]
    public void Validate_MonthTooSmall_IsInvalid()
    {
        var result = _validator.TestValidate(new GetDashboardSummaryRequest(2026, 0));
        result.ShouldHaveValidationErrorFor(x => x.Month);
    }

    [Fact]
    public void Validate_MonthAtLowerBound_IsValid()
    {
        var result = _validator.TestValidate(new GetDashboardSummaryRequest(2026, 1));
        result.ShouldNotHaveValidationErrorFor(x => x.Month);
    }

    [Fact]
    public void Validate_MonthAtUpperBound_IsValid()
    {
        var result = _validator.TestValidate(new GetDashboardSummaryRequest(2026, 12));
        result.ShouldNotHaveValidationErrorFor(x => x.Month);
    }

    [Fact]
    public void Validate_MonthTooLarge_IsInvalid()
    {
        var result = _validator.TestValidate(new GetDashboardSummaryRequest(2026, 13));
        result.ShouldHaveValidationErrorFor(x => x.Month);
    }
}
