import { apiFetch } from './client'

/** カテゴリの種別 */
export type CategoryType = 'Income' | 'Expense'

export type Category = {
  id: string
  name: string
  type: CategoryType
  /** ISO 8601形式 */
  createdAt: string
}

export type CategoryBody = {
  name: string
  type: CategoryType
}

/**
 * ログインユーザーのカテゴリ一覧を取得する
 * @returns カテゴリの配列
 */
export function getCategories(): Promise<Category[]> {
  return apiFetch<Category[]>('/api/categories')
}

/**
 * カテゴリを新規作成する
 * @param body カテゴリ名と種別
 * @returns 作成されたカテゴリ
 */
export function createCategory(body: CategoryBody): Promise<Category> {
  return apiFetch<Category>('/api/categories', {
    method: 'POST',
    body: JSON.stringify(body),
  })
}

/**
 * カテゴリを更新する
 * @param id 更新対象のカテゴリID
 * @param body 新しいカテゴリ名と種別
 * @returns 更新後のカテゴリ
 */
export function updateCategory(id: string, body: CategoryBody): Promise<Category> {
  return apiFetch<Category>(`/api/categories/${id}`, {
    method: 'PUT',
    body: JSON.stringify(body),
  })
}

/**
 * カテゴリを削除する
 * @param id 削除対象のカテゴリID
 */
export function deleteCategory(id: string): Promise<void> {
  return apiFetch<void>(`/api/categories/${id}`, { method: 'DELETE' })
}
