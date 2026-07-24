import { apiFetch } from './client'

export type CategoryBreakdownItem = {
  categoryName: string
  amount: number
  /** 支出に占める割合 (0–100) */
  percentage: number
}

export type RecentTransactionItem = {
  id: string
  categoryName: string
  type: 'Income' | 'Expense'
  amount: number
  /** 'YYYY-MM-DD' */
  date: string
  memo: string | null
}

export type DashboardSummaryResponse = {
  totalIncome: number
  totalExpense: number
  balance: number
  categoryBreakdown: CategoryBreakdownItem[]
  recentTransactions: RecentTransactionItem[]
}

/**
 * 指定月のダッシュボード集計を取得する
 * @param year 対象年
 * @param month 対象月 (1–12)
 * @returns ダッシュボード集計データ
 */
export function getDashboardSummary(year: number, month: number): Promise<DashboardSummaryResponse> {
  return apiFetch<DashboardSummaryResponse>(
    `/api/dashboard/summary?year=${year}&month=${month}`,
  )
}
