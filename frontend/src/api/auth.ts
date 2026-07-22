import { apiFetch } from './client'

export type LoginRequest = {
  email: string
  password: string
}

export type AuthResponse = {
  /** アクセストークン(Cookie)の有効期限 (ISO 8601形式) */
  accessTokenExpiresAt: string
}

/**
 * ログインする。成功時はアクセストークン・リフレッシュトークンが httpOnly Cookie にセットされる
 * @param body メールアドレスとパスワード
 * @returns アクセストークンの有効期限情報
 */
export async function login(body: LoginRequest): Promise<AuthResponse> {
  return apiFetch<AuthResponse>('/api/auth/login', {
    method: 'POST',
    body: JSON.stringify(body),
  })
}

/**
 * リフレッシュトークン(Cookie)を使ってアクセストークンを更新する
 * @returns 更新後のアクセストークンの有効期限情報
 */
export async function refresh(): Promise<AuthResponse> {
  return apiFetch<AuthResponse>('/api/auth/refresh', { method: 'POST' })
}

/**
 * ログアウトする
 */
export async function logout(): Promise<void> {
  return apiFetch<void>('/api/auth/logout', { method: 'POST' })
}
