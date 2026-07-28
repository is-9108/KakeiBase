import { apiFetch } from './client'

export type Subscription = {
  id: string
  categoryId: string
  name: string
  /** 月額料金（円、正の整数） */
  amount: number
  /** 有効フラグ。false の場合は停止中 */
  isActive: boolean
  /** ISO 8601形式 */
  createdAt: string
  /** ISO 8601形式 */
  updatedAt: string
}

export type SubscriptionBody = {
  categoryId: string
  name: string
  amount: number
}

export type SubscriptionUpdateBody = SubscriptionBody & {
  isActive: boolean
}

/**
 * ログインユーザーのサブスク一覧を取得する
 * @returns サブスクの配列
 */
export function getSubscriptions(): Promise<Subscription[]> {
  return apiFetch<Subscription[]>('/api/subscriptions')
}

/**
 * サブスクを新規作成する
 * @param body カテゴリ・サービス名・金額
 * @returns 作成されたサブスク
 */
export function createSubscription(body: SubscriptionBody): Promise<Subscription> {
  return apiFetch<Subscription>('/api/subscriptions', {
    method: 'POST',
    body: JSON.stringify(body),
  })
}

/**
 * サブスクを更新する(停止/再開もこの関数で isActive を切り替えて行う)
 * @param id 更新対象のサブスクID
 * @param body 新しいカテゴリ・サービス名・金額・有効フラグ
 * @returns 更新後のサブスク
 */
export function updateSubscription(id: string, body: SubscriptionUpdateBody): Promise<Subscription> {
  return apiFetch<Subscription>(`/api/subscriptions/${id}`, {
    method: 'PUT',
    body: JSON.stringify(body),
  })
}

/**
 * サブスクを削除する
 * @param id 削除対象のサブスクID
 */
export function deleteSubscription(id: string): Promise<void> {
  return apiFetch<void>(`/api/subscriptions/${id}`, { method: 'DELETE' })
}
