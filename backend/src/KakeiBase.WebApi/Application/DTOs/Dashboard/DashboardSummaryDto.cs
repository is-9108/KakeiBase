using KakeiBase.WebApi.Domain.Enums;

namespace KakeiBase.WebApi.Application.DTOs.Dashboard;

/// <summary>ダッシュボード集計結果</summary>
/// <param name="TotalIncome">指定月の収入合計（円）</param>
/// <param name="TotalExpense">指定月の支出合計（円）</param>
/// <param name="Balance">残高（収入合計 - 支出合計）（円）</param>
/// <param name="CategoryBreakdown">支出のカテゴリ別内訳</param>
/// <param name="RecentTransactions">月内の直近取引（最大5件、日付降順）</param>
public record DashboardSummaryDto(
    int TotalIncome,
    int TotalExpense,
    int Balance,
    List<CategoryBreakdownDto> CategoryBreakdown,
    List<RecentTransactionDto> RecentTransactions);

/// <summary>カテゴリ別支出内訳</summary>
/// <param name="CategoryName">カテゴリ名</param>
/// <param name="Amount">支出合計（円）</param>
/// <param name="Percentage">支出全体に占める割合（%）</param>
public record CategoryBreakdownDto(string CategoryName, int Amount, decimal Percentage);

/// <summary>直近取引の概要</summary>
/// <param name="Id">取引ID</param>
/// <param name="CategoryName">カテゴリ名</param>
/// <param name="Type">収支区分</param>
/// <param name="Amount">金額（円）</param>
/// <param name="Date">発生日付</param>
/// <param name="Memo">メモ</param>
public record RecentTransactionDto(
    Guid Id,
    string CategoryName,
    TransactionType Type,
    int Amount,
    DateOnly Date,
    string? Memo);
