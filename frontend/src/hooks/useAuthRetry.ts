import { useNavigate } from 'react-router-dom'
import { refresh } from '../api/auth'
import { ApiError } from '../api/client'

/**
 * 401エラー時にトークンリフレッシュを試み、失敗した場合は /login へリダイレクトする
 * クエリ関数ラッパーを返すカスタムHook
 */
export function useAuthRetry() {
  const navigate = useNavigate()
  return function withAuthRetry<T>(fetchFn: () => Promise<T>): Promise<T> {
    return fetchFn().catch(async (e: unknown) => {
      if (e instanceof ApiError && e.status === 401) {
        try {
          await refresh()
          return await fetchFn()
        } catch {
          navigate('/login', { replace: true })
          throw e
        }
      }
      throw e
    })
  }
}
