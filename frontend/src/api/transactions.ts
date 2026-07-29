import { apiFetch } from './client'

export type Transaction = {
  id: string
  categoryId: string
  categoryName: string
  subscriptionId: string | null
  /** 金額（円、正の整数） */
  amount: number
  /** 取引日（YYYY-MM-DD形式） */
  date: string
  memo: string | null
  receiptS3Key: string | null
  /** ISO 8601形式 */
  createdAt: string
  /** ISO 8601形式 */
  updatedAt: string
}

export type TransactionBody = {
  categoryId: string
  /** 金額（円、正の整数） */
  amount: number
  /** 取引日（YYYY-MM-DD形式） */
  date: string
  memo?: string | null
}

/**
 * 指定年月の収支一覧を取得する
 * @param year 対象年
 * @param month 対象月
 * @returns 収支の配列
 */
export function getTransactions(year: number, month: number): Promise<Transaction[]> {
  return apiFetch<Transaction[]>(`/api/transactions?year=${year}&month=${month}`)
}

/**
 * 収支を新規作成する
 * @param body カテゴリID・金額・日付・メモ
 * @returns 作成された収支
 */
export function createTransaction(body: TransactionBody): Promise<Transaction> {
  return apiFetch<Transaction>('/api/transactions', {
    method: 'POST',
    body: JSON.stringify(body),
  })
}

/**
 * 収支を更新する
 * @param id 更新対象の収支ID
 * @param body 新しいカテゴリID・金額・日付・メモ
 * @returns 更新後の収支
 */
export function updateTransaction(id: string, body: TransactionBody): Promise<Transaction> {
  return apiFetch<Transaction>(`/api/transactions/${id}`, {
    method: 'PUT',
    body: JSON.stringify(body),
  })
}

/**
 * 収支を削除する
 * @param id 削除対象の収支ID
 */
export function deleteTransaction(id: string): Promise<void> {
  return apiFetch<void>(`/api/transactions/${id}`, { method: 'DELETE' })
}
