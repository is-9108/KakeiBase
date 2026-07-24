import { describe, it, expect, vi, beforeEach, afterEach } from 'vitest'
import { render, screen, fireEvent } from '@testing-library/react'
import { QueryClient, QueryClientProvider } from '@tanstack/react-query'
import { MemoryRouter, Route, Routes } from 'react-router-dom'
import DashboardPage from './DashboardPage'
import { getDashboardSummary } from '../api/dashboard'
import { logout, refresh } from '../api/auth'
import { ApiError } from '../api/client'
import type { DashboardSummaryResponse } from '../api/dashboard'

vi.mock('../api/dashboard')
vi.mock('../api/auth')

const mockGetDashboardSummary = vi.mocked(getDashboardSummary)
const mockRefresh = vi.mocked(refresh)
const mockLogout = vi.mocked(logout)

const mockData: DashboardSummaryResponse = {
  totalIncome: 300000,
  totalExpense: 120000,
  balance: 180000,
  categoryBreakdown: [
    { categoryName: '食費', amount: 50000, percentage: 33 },
    { categoryName: '交通費', amount: 100000, percentage: 67 },
  ],
  recentTransactions: [
    { id: '1', categoryName: '食費', type: 'Expense', amount: 5000, date: '2026-07-01', memo: null },
  ],
}

function renderDashboardPage() {
  const queryClient = new QueryClient({
    defaultOptions: { queries: { retry: false } },
  })
  return render(
    <QueryClientProvider client={queryClient}>
      <MemoryRouter initialEntries={['/']}>
        <Routes>
          <Route path="/" element={<DashboardPage />} />
          <Route path="/login" element={<div>Login</div>} />
        </Routes>
      </MemoryRouter>
    </QueryClientProvider>,
  )
}

beforeEach(() => {
  vi.resetAllMocks()
})

describe('DashboardPage', () => {
  it('データ取得成功時にサマリーカードと取引一覧を表示する', async () => {
    mockGetDashboardSummary.mockResolvedValue(mockData)

    renderDashboardPage()

    expect(await screen.findByText('300,000円')).toBeInTheDocument()
    expect(screen.getByText('120,000円')).toBeInTheDocument()
    expect(screen.getByText('180,000円')).toBeInTheDocument()
    expect(screen.getAllByText('食費').length).toBeGreaterThan(0)
    expect(screen.getByText('2026-07-01')).toBeInTheDocument()
  })

  it('読み込み中はスピナーを表示し、完了後は消える', async () => {
    let resolveData: (value: DashboardSummaryResponse) => void = () => {}
    mockGetDashboardSummary.mockReturnValue(new Promise((r) => { resolveData = r }))

    renderDashboardPage()

    expect(screen.getByRole('status')).toBeInTheDocument()

    resolveData(mockData)
    await screen.findByText('300,000円')

    expect(screen.queryByRole('status')).not.toBeInTheDocument()
  })

  it('401エラー後にrefresh成功したらデータを再取得して表示する', async () => {
    mockGetDashboardSummary
      .mockRejectedValueOnce(new ApiError(401, {}))
      .mockResolvedValueOnce(mockData)
    mockRefresh.mockResolvedValue({ accessTokenExpiresAt: '2026-07-24T00:00:00Z' })

    renderDashboardPage()

    expect(await screen.findByText('300,000円')).toBeInTheDocument()
    expect(mockRefresh).toHaveBeenCalledTimes(1)
    expect(mockGetDashboardSummary).toHaveBeenCalledTimes(2)
  })

  it('401エラー後にrefreshも失敗したら/loginへリダイレクトする', async () => {
    mockGetDashboardSummary.mockRejectedValue(new ApiError(401, {}))
    mockRefresh.mockRejectedValue(new ApiError(401, {}))

    renderDashboardPage()

    expect(await screen.findByText('Login')).toBeInTheDocument()
  })

  it('非401エラー時は/loginへリダイレクトせずトーストを表示する', async () => {
    mockGetDashboardSummary.mockRejectedValue(new ApiError(500, { title: 'Server Error' }))

    renderDashboardPage()

    expect(await screen.findByText('データの取得に失敗しました')).toBeInTheDocument()
    expect(screen.queryByText('Login')).not.toBeInTheDocument()
  })

  describe('月跨ぎナビゲーション', () => {
    afterEach(() => {
      vi.useRealTimers()
    })

    it('1月に「前月」を押すと前年12月に切り替わる', () => {
      vi.useFakeTimers()
      vi.setSystemTime(new Date(2026, 0, 1)) // 2026年1月
      mockGetDashboardSummary.mockResolvedValue(mockData)

      renderDashboardPage()

      expect(screen.getByText('2026年1月')).toBeInTheDocument()

      fireEvent.click(screen.getByRole('button', { name: '◄' }))

      expect(screen.getByText('2025年12月')).toBeInTheDocument()
    })

    it('12月に「次月」を押すと翌年1月に切り替わる', () => {
      vi.useFakeTimers()
      vi.setSystemTime(new Date(2026, 11, 1)) // 2026年12月
      mockGetDashboardSummary.mockResolvedValue(mockData)

      renderDashboardPage()

      expect(screen.getByText('2026年12月')).toBeInTheDocument()

      fireEvent.click(screen.getByRole('button', { name: '►' }))

      expect(screen.getByText('2027年1月')).toBeInTheDocument()
    })
  })

  it('ログアウトボタン押下でlogoutを呼び/loginへ遷移する', async () => {
    mockGetDashboardSummary.mockResolvedValue(mockData)
    mockLogout.mockResolvedValue(undefined)
    const { getByRole } = renderDashboardPage()

    const logoutButton = getByRole('button', { name: 'ログアウト' })
    logoutButton.click()

    expect(await screen.findByText('Login')).toBeInTheDocument()
    expect(mockLogout).toHaveBeenCalledTimes(1)
  })
})
