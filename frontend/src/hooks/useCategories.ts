import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query'
import {
  getCategories,
  createCategory,
  updateCategory,
  deleteCategory,
  type CategoryBody,
} from '../api/categories'
import { useAuthRetry } from './useAuthRetry'

const QUERY_KEY = ['categories'] as const

/**
 * ログインユーザーのカテゴリ一覧を取得するカスタムフック。
 * 401 エラー時はトークンリフレッシュを試み、失敗した場合は /login へリダイレクトする。
 */
export function useCategories() {
  const withAuthRetry = useAuthRetry()
  return useQuery({
    queryKey: QUERY_KEY,
    queryFn: () => withAuthRetry(getCategories),
    retry: false,
  })
}

/**
 * カテゴリを新規作成するミューテーションフック。
 * 成功時にカテゴリ一覧のキャッシュを無効化する。
 */
export function useCreateCategory() {
  const queryClient = useQueryClient()
  return useMutation({
    mutationFn: createCategory,
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: QUERY_KEY })
    },
  })
}

/**
 * カテゴリを更新するミューテーションフック。
 * 成功時にカテゴリ一覧のキャッシュを無効化する。
 */
export function useUpdateCategory() {
  const queryClient = useQueryClient()
  return useMutation({
    mutationFn: ({ id, body }: { id: string; body: CategoryBody }) =>
      updateCategory(id, body),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: QUERY_KEY })
    },
  })
}

/**
 * カテゴリを削除するミューテーションフック。
 * 成功時にカテゴリ一覧のキャッシュを無効化する。
 */
export function useDeleteCategory() {
  const queryClient = useQueryClient()
  return useMutation({
    mutationFn: deleteCategory,
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: QUERY_KEY })
    },
  })
}
