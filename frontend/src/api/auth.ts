import { apiFetch } from './client'

export type LoginRequest = {
  email: string
  password: string
}

export type UserProfile = {
  id: string
  email: string
}

/**
 * ログインする
 * @param body メールアドレスとパスワード
 * @returns ログインしたユーザーのプロフィール
 */
export async function login(body: LoginRequest): Promise<UserProfile> {
  return apiFetch<UserProfile>('/api/auth/login', {
    method: 'POST',
    body: JSON.stringify(body),
  })
}

/**
 * ログアウトする
 */
export async function logout(): Promise<void> {
  return apiFetch<void>('/api/auth/logout', { method: 'POST' })
}

/**
 * 現在ログイン中のユーザープロフィールを取得する
 * @returns ログイン中のユーザー情報
 */
export async function getMe(): Promise<UserProfile> {
  return apiFetch<UserProfile>('/api/auth/me')
}
