import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query'
import { useNavigate } from 'react-router-dom'
import {
  getTransactions,
  createTransaction,
  updateTransaction,
  deleteTransaction,
  type TransactionBody,
} from '../api/transactions'
import { refresh } from '../api/auth'
import { ApiError } from '../api/client'

const QUERY_KEY = 'transactions' as const

/**
 * 指定年月の収支一覧を取得するカスタムフック。
 * 401 エラー時はトークンリフレッシュを試み、失敗した場合は /login へリダイレクトする。
 * @param year 対象年
 * @param month 対象月
 */
export function useTransactions(year: number, month: number) {
  const navigate = useNavigate()
  return useQuery({
    queryKey: [QUERY_KEY, year, month],
    queryFn: async () => {
      try {
        return await getTransactions(year, month)
      } catch (e) {
        if (e instanceof ApiError && e.status === 401) {
          try {
            await refresh()
            return await getTransactions(year, month)
          } catch {
            navigate('/login', { replace: true })
            throw e
          }
        }
        throw e
      }
    },
    retry: false,
  })
}

/**
 * 収支を新規作成するミューテーションフック。
 * 成功時に収支一覧のキャッシュを無効化する。
 */
export function useCreateTransaction() {
  const queryClient = useQueryClient()
  return useMutation({
    mutationFn: createTransaction,
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: [QUERY_KEY] })
    },
  })
}

/**
 * 収支を更新するミューテーションフック。
 * 成功時に収支一覧のキャッシュを無効化する。
 */
export function useUpdateTransaction() {
  const queryClient = useQueryClient()
  return useMutation({
    mutationFn: ({ id, body }: { id: string; body: TransactionBody }) =>
      updateTransaction(id, body),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: [QUERY_KEY] })
    },
  })
}

/**
 * 収支を削除するミューテーションフック。
 * 成功時に収支一覧のキャッシュを無効化する。
 */
export function useDeleteTransaction() {
  const queryClient = useQueryClient()
  return useMutation({
    mutationFn: deleteTransaction,
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: [QUERY_KEY] })
    },
  })
}
