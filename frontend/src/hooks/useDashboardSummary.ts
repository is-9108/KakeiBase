import { useQuery } from '@tanstack/react-query'
import { useNavigate } from 'react-router-dom'
import { getDashboardSummary } from '../api/dashboard'
import { refresh } from '../api/auth'
import { ApiError } from '../api/client'

/**
 * 指定月のダッシュボード集計を取得するカスタムフック。
 * 401 エラー時はトークンリフレッシュを試み、失敗した場合は /login へリダイレクトする。
 * @param year 対象年
 * @param month 対象月 (1–12)
 */
export function useDashboardSummary(year: number, month: number) {
  const navigate = useNavigate()
  return useQuery({
    queryKey: ['dashboardSummary', year, month],
    queryFn: async () => {
      try {
        return await getDashboardSummary(year, month)
      } catch (e) {
        if (e instanceof ApiError && e.status === 401) {
          try {
            await refresh()
            return await getDashboardSummary(year, month)
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
