using KakeiBase.WebApi.Application.DTOs.Dashboard;
using KakeiBase.WebApi.Application.Interfaces;
using KakeiBase.WebApi.Domain.Enums;

namespace KakeiBase.WebApi.Application.UseCases.Dashboard;

/// <summary>指定月のダッシュボード集計を返すユースケース</summary>
public class GetDashboardSummaryUseCase(
    ITransactionRepository transactionRepository,
    ICategoryRepository categoryRepository)
{
    /// <summary>指定ユーザー・年月のダッシュボード集計を実行する</summary>
    /// <param name="userId">集計対象ユーザーのID</param>
    /// <param name="year">集計対象年</param>
    /// <param name="month">集計対象月（1–12）</param>
    /// <param name="ct">キャンセルトークン</param>
    /// <returns>収入合計・支出合計・残高・カテゴリ別内訳・直近取引を含む集計結果</returns>
    public async Task<DashboardSummaryDto> ExecuteAsync(
        Guid userId, int year, int month, CancellationToken ct = default)
    {
        var transactions = await transactionRepository
            .FindAllByUserIdAsync(userId, year, month, null, ct);
        var categories = await categoryRepository
            .FindAllByUserIdAsync(userId, ct);
        var categoryMap = categories.ToDictionary(c => c.Id, c => c.Name);

        var totalIncome = transactions
            .Where(t => t.Type == TransactionType.Income)
            .Sum(t => t.Amount);
        var totalExpense = transactions
            .Where(t => t.Type == TransactionType.Expense)
            .Sum(t => t.Amount);
        var balance = totalIncome - totalExpense;

        // カテゴリ別内訳（支出のみ）、金額降順
        var breakdown = transactions
            .Where(t => t.Type == TransactionType.Expense)
            .GroupBy(t => t.CategoryId)
            .Select(g => new CategoryBreakdownDto(
                categoryMap.GetValueOrDefault(g.Key, "不明"),
                g.Sum(t => t.Amount),
                totalExpense == 0
                    ? 0m
                    : Math.Round((decimal)g.Sum(t => t.Amount) / totalExpense * 100, 1)))
            .OrderByDescending(c => c.Amount)
            .ToList();

        // 直近取引（FindAllByUserIdAsync は date 降順で返るため先頭 5 件を取る）
        var recent = transactions
            .Take(5)
            .Select(t => new RecentTransactionDto(
                t.Id,
                categoryMap.GetValueOrDefault(t.CategoryId, "不明"),
                t.Type,
                t.Amount,
                t.Date,
                t.Memo))
            .ToList();

        return new DashboardSummaryDto(totalIncome, totalExpense, balance, breakdown, recent);
    }
}
