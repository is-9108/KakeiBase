import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query'
import {
  getSubscriptions,
  createSubscription,
  updateSubscription,
  deleteSubscription,
  type SubscriptionUpdateBody,
} from '../api/subscriptions'
import { useAuthRetry } from './useAuthRetry'

const QUERY_KEY = ['subscriptions'] as const

/**
 * ログインユーザーのサブスク一覧を取得するカスタムフック。
 * 401 エラー時はトークンリフレッシュを試み、失敗した場合は /login へリダイレクトする。
 */
export function useSubscriptions() {
  const withAuthRetry = useAuthRetry()
  return useQuery({
    queryKey: QUERY_KEY,
    queryFn: () => withAuthRetry(getSubscriptions),
    retry: false,
  })
}

/**
 * サブスクを新規作成するミューテーションフック。
 * 成功時にサブスク一覧のキャッシュを無効化する。
 */
export function useCreateSubscription() {
  const queryClient = useQueryClient()
  return useMutation({
    mutationFn: createSubscription,
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: QUERY_KEY })
    },
  })
}

/**
 * サブスクを更新するミューテーションフック(停止/再開もこれを使う)。
 * 成功時にサブスク一覧のキャッシュを無効化する。
 */
export function useUpdateSubscription() {
  const queryClient = useQueryClient()
  return useMutation({
    mutationFn: ({ id, body }: { id: string; body: SubscriptionUpdateBody }) =>
      updateSubscription(id, body),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: QUERY_KEY })
    },
  })
}

/**
 * サブスクを削除するミューテーションフック。
 * 成功時にサブスク一覧のキャッシュを無効化する。
 */
export function useDeleteSubscription() {
  const queryClient = useQueryClient()
  return useMutation({
    mutationFn: deleteSubscription,
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: QUERY_KEY })
    },
  })
}
