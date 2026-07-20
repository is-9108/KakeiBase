/** RFC 7807 Problem Details レスポンス */
type ProblemDetails = {
  type?: string
  title?: string
  status?: number
  detail?: string
  instance?: string
}

export class ApiError extends Error {
  constructor(
    public readonly status: number,
    public readonly problem: ProblemDetails,
  ) {
    super(problem.detail ?? problem.title ?? `HTTP ${status}`)
    this.name = 'ApiError'
  }
}

const BASE_URL = import.meta.env['VITE_API_BASE_URL'] ?? ''

/**
 * バックエンドAPIへのfetchラッパー
 * - credentials: 'include' でCookieを自動送信
 * - 200系以外のレスポンスはRFC 7807 Problem Detailsとしてエラーを投げる
 * @param path APIパス (例: '/api/expenses')
 * @param init fetchオプション
 * @returns レスポンスのJSONをパースした値
 */
export async function apiFetch<T>(path: string, init?: RequestInit): Promise<T> {
  const response = await fetch(`${BASE_URL}${path}`, {
    ...init,
    credentials: 'include',
    headers: {
      'Content-Type': 'application/json',
      ...init?.headers,
    },
  })

  if (!response.ok) {
    const problem: ProblemDetails = await response.json().catch(() => ({}))
    throw new ApiError(response.status, problem)
  }

  if (response.status === 204) {
    return undefined as T
  }

  return response.json() as Promise<T>
}
