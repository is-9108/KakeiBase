import { describe, it, expect, vi, beforeEach } from 'vitest'
import { render, screen } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { MemoryRouter, Route, Routes } from 'react-router-dom'
import LoginPage from './LoginPage'
import { login, refresh } from '../api/auth'
import { ApiError } from '../api/client'

vi.mock('../api/auth')

const mockLogin = vi.mocked(login)
const mockRefresh = vi.mocked(refresh)

function renderLoginPage() {
  return render(
    <MemoryRouter initialEntries={['/login']}>
      <Routes>
        <Route path="/login" element={<LoginPage />} />
        <Route path="/" element={<div>Dashboard</div>} />
      </Routes>
    </MemoryRouter>,
  )
}

beforeEach(() => {
  vi.resetAllMocks()
  mockRefresh.mockRejectedValue(new ApiError(401, {}))
})

describe('LoginPage', () => {
  it('メールアドレス・パスワード・ログインボタンを表示する', async () => {
    renderLoginPage()
    expect(await screen.findByLabelText('メールアドレス')).toBeInTheDocument()
    expect(screen.getByLabelText('パスワード')).toBeInTheDocument()
    expect(screen.getByRole('button', { name: 'ログイン' })).toBeInTheDocument()
  })

  it('空欄のまま送信するとバリデーションエラーが表示され、loginは呼ばれない', async () => {
    const user = userEvent.setup()
    renderLoginPage()
    await screen.findByLabelText('メールアドレス')

    await user.click(screen.getByRole('button', { name: 'ログイン' }))

    expect(await screen.findByText('メールアドレスを入力してください')).toBeInTheDocument()
    expect(screen.getByText('パスワードを入力してください')).toBeInTheDocument()
    expect(mockLogin).not.toHaveBeenCalled()
  })

  it('不正なメール形式で送信するとバリデーションエラーが表示され、loginは呼ばれない', async () => {
    const user = userEvent.setup()
    renderLoginPage()
    await screen.findByLabelText('メールアドレス')

    await user.type(screen.getByLabelText('メールアドレス'), 'not-an-email')
    await user.type(screen.getByLabelText('パスワード'), 'password123')
    await user.click(screen.getByRole('button', { name: 'ログイン' }))

    expect(
      await screen.findByText('メールアドレスの形式が正しくありません'),
    ).toBeInTheDocument()
    expect(mockLogin).not.toHaveBeenCalled()
  })

  it('ログイン成功時は / へ遷移する', async () => {
    mockLogin.mockResolvedValue({ accessTokenExpiresAt: '2026-07-23T10:15:00Z' })
    const user = userEvent.setup()
    renderLoginPage()
    await screen.findByLabelText('メールアドレス')

    await user.type(screen.getByLabelText('メールアドレス'), 'user@example.com')
    await user.type(screen.getByLabelText('パスワード'), 'password123')
    await user.click(screen.getByRole('button', { name: 'ログイン' }))

    expect(await screen.findByText('Dashboard')).toBeInTheDocument()
  })

  it('ログイン失敗時は詳細を出さず汎用エラーメッセージを表示する', async () => {
    mockLogin.mockRejectedValue(new ApiError(401, { title: 'Unauthorized' }))
    const user = userEvent.setup()
    renderLoginPage()
    await screen.findByLabelText('メールアドレス')

    await user.type(screen.getByLabelText('メールアドレス'), 'user@example.com')
    await user.type(screen.getByLabelText('パスワード'), 'wrong-password')
    await user.click(screen.getByRole('button', { name: 'ログイン' }))

    expect(await screen.findByText('ログインに失敗しました')).toBeInTheDocument()
    expect(screen.queryByText('Unauthorized')).not.toBeInTheDocument()
  })

  it('マウント時にrefreshが成功したら認証済みとみなし、フォームを表示せず / へ遷移する', async () => {
    mockRefresh.mockResolvedValue({ accessTokenExpiresAt: '2026-07-23T10:15:00Z' })
    renderLoginPage()

    expect(await screen.findByText('Dashboard')).toBeInTheDocument()
    expect(screen.queryByLabelText('メールアドレス')).not.toBeInTheDocument()
  })

  it('マウント時にrefreshが失敗したらログインフォームを表示する', async () => {
    renderLoginPage()
    expect(await screen.findByLabelText('メールアドレス')).toBeInTheDocument()
  })

  it('送信中に連続でsubmitイベントが発火してもloginは1回しか呼ばれない', async () => {
    let resolveLogin: (value: { accessTokenExpiresAt: string }) => void = () => {}
    mockLogin.mockReturnValue(
      new Promise((resolve) => {
        resolveLogin = resolve
      }),
    )
    const user = userEvent.setup()
    renderLoginPage()
    await screen.findByLabelText('メールアドレス')

    await user.type(screen.getByLabelText('メールアドレス'), 'user@example.com')
    await user.type(screen.getByLabelText('パスワード'), 'password123')

    const form = screen.getByRole('button', { name: 'ログイン' }).closest('form')
    if (!form) throw new Error('form not found')
    form.requestSubmit()
    form.requestSubmit()

    expect(mockLogin).toHaveBeenCalledTimes(1)
    resolveLogin({ accessTokenExpiresAt: '2026-07-23T10:15:00Z' })
    expect(await screen.findByText('Dashboard')).toBeInTheDocument()
  })
})
