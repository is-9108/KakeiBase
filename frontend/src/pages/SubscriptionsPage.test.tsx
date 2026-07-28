import { describe, it, expect, vi, beforeEach, afterEach } from 'vitest'
import { render, screen, fireEvent, waitFor, within } from '@testing-library/react'
import { QueryClient, QueryClientProvider } from '@tanstack/react-query'
import { MemoryRouter, Route, Routes } from 'react-router-dom'
import SubscriptionsPage from './SubscriptionsPage'
import {
  getSubscriptions,
  createSubscription,
  updateSubscription,
  deleteSubscription,
} from '../api/subscriptions'
import { getCategories } from '../api/categories'
import { logout, refresh } from '../api/auth'
import { ApiError } from '../api/client'
import type { Subscription } from '../api/subscriptions'
import type { Category } from '../api/categories'

vi.mock('../api/subscriptions')
vi.mock('../api/categories')
vi.mock('../api/auth')

const mockGetSubscriptions = vi.mocked(getSubscriptions)
const mockCreateSubscription = vi.mocked(createSubscription)
const mockUpdateSubscription = vi.mocked(updateSubscription)
const mockDeleteSubscription = vi.mocked(deleteSubscription)
const mockGetCategories = vi.mocked(getCategories)
const mockRefresh = vi.mocked(refresh)
const mockLogout = vi.mocked(logout)

const mockCategories: Category[] = [
  { id: 'cat-1', name: '娯楽', type: 'Expense', createdAt: '2026-01-01T00:00:00Z' },
  { id: 'cat-2', name: '給与', type: 'Income', createdAt: '2026-01-01T00:00:00Z' },
]

const mockSubscriptions: Subscription[] = [
  {
    id: 'sub-1',
    categoryId: 'cat-1',
    name: 'Netflix',
    amount: 1490,
    isActive: true,
    createdAt: '2026-01-01T00:00:00Z',
    updatedAt: '2026-01-01T00:00:00Z',
  },
  {
    id: 'sub-2',
    categoryId: 'cat-1',
    name: 'Disney+',
    amount: 990,
    isActive: false,
    createdAt: '2026-01-01T00:00:00Z',
    updatedAt: '2026-01-01T00:00:00Z',
  },
]

function renderSubscriptionsPage() {
  const queryClient = new QueryClient({
    defaultOptions: { queries: { retry: false } },
  })
  return render(
    <QueryClientProvider client={queryClient}>
      <MemoryRouter initialEntries={['/subscriptions']}>
        <Routes>
          <Route path="/subscriptions" element={<SubscriptionsPage />} />
          <Route path="/login" element={<div>Login</div>} />
          <Route path="/" element={<div>Dashboard</div>} />
        </Routes>
      </MemoryRouter>
    </QueryClientProvider>,
  )
}

beforeEach(() => {
  vi.resetAllMocks()
  mockLogout.mockResolvedValue(undefined)
})

afterEach(() => {
  vi.restoreAllMocks()
})

describe('SubscriptionsPage', () => {
  it('有効なサブスクと停止中のサブスクをそれぞれのテーブルに表示する', async () => {
    mockGetSubscriptions.mockResolvedValue(mockSubscriptions)
    mockGetCategories.mockResolvedValue(mockCategories)

    renderSubscriptionsPage()

    expect(await screen.findByRole('cell', { name: 'Netflix' })).toBeInTheDocument()
    expect(screen.getByRole('cell', { name: 'Disney+' })).toBeInTheDocument()
    expect(screen.getByText('¥1,490')).toBeInTheDocument()
    expect(screen.getByText('¥990')).toBeInTheDocument()
    expect(screen.getAllByRole('cell', { name: '娯楽' })).toHaveLength(2)
  })

  it('サブスクが0件のとき有効・停止中それぞれに空状態文言を表示する', async () => {
    mockGetSubscriptions.mockResolvedValue([])
    mockGetCategories.mockResolvedValue(mockCategories)

    renderSubscriptionsPage()

    expect(await screen.findByText('有効なサブスクはありません')).toBeInTheDocument()
    expect(screen.getByText('停止中のサブスクはありません')).toBeInTheDocument()
  })

  it('読み込み中はスピナーを表示し、完了後は消える', async () => {
    let resolve: (value: Subscription[]) => void = () => {}
    mockGetSubscriptions.mockReturnValue(new Promise((r) => { resolve = r }))
    mockGetCategories.mockResolvedValue(mockCategories)

    renderSubscriptionsPage()

    expect(screen.getByRole('status')).toBeInTheDocument()

    resolve(mockSubscriptions)
    await screen.findByRole('cell', { name: 'Netflix' })

    expect(screen.queryByRole('status')).not.toBeInTheDocument()
  })

  it('401 → refresh 成功 → 一覧表示する', async () => {
    mockGetSubscriptions
      .mockRejectedValueOnce(new ApiError(401, {}))
      .mockResolvedValueOnce(mockSubscriptions)
    mockRefresh.mockResolvedValue({ accessTokenExpiresAt: '2026-07-28T00:00:00Z' })
    mockGetCategories.mockResolvedValue(mockCategories)

    renderSubscriptionsPage()

    expect(await screen.findByRole('cell', { name: 'Netflix' })).toBeInTheDocument()
    expect(mockRefresh).toHaveBeenCalledTimes(1)
  })

  it('401 → refresh 失敗 → /login へリダイレクト', async () => {
    mockGetSubscriptions.mockRejectedValue(new ApiError(401, {}))
    mockRefresh.mockRejectedValue(new ApiError(401, {}))
    mockGetCategories.mockResolvedValue(mockCategories)

    renderSubscriptionsPage()

    expect(await screen.findByText('Login')).toBeInTheDocument()
  })

  it('非401エラー時はトーストを表示する', async () => {
    mockGetSubscriptions.mockRejectedValue(new ApiError(500, { title: 'Server Error' }))
    mockGetCategories.mockResolvedValue(mockCategories)

    renderSubscriptionsPage()

    expect(await screen.findByText('データの取得に失敗しました')).toBeInTheDocument()
    expect(screen.queryByText('Login')).not.toBeInTheDocument()
  })

  it('+ 追加ボタンを押すとモーダルが表示される', async () => {
    mockGetSubscriptions.mockResolvedValue([])
    mockGetCategories.mockResolvedValue(mockCategories)

    renderSubscriptionsPage()
    await screen.findByText('有効なサブスクはありません')

    fireEvent.click(screen.getByRole('button', { name: '+ 追加' }))

    expect(screen.getByRole('dialog', { name: 'サブスク追加' })).toBeInTheDocument()
  })

  it('追加モーダルのカテゴリ選択肢は支出カテゴリのみを含む', async () => {
    mockGetSubscriptions.mockResolvedValue([])
    mockGetCategories.mockResolvedValue(mockCategories)

    renderSubscriptionsPage()
    await screen.findByText('有効なサブスクはありません')
    fireEvent.click(screen.getByRole('button', { name: '+ 追加' }))

    const categorySelect = screen.getByRole('combobox', { name: 'カテゴリ' })
    expect(within(categorySelect).getByText('娯楽')).toBeInTheDocument()
    expect(within(categorySelect).queryByText('給与')).not.toBeInTheDocument()
  })

  it('追加フォーム送信 → createSubscription が呼ばれ成功トーストを表示する', async () => {
    const newSub: Subscription = {
      id: 'sub-3',
      categoryId: 'cat-1',
      name: 'Spotify',
      amount: 980,
      isActive: true,
      createdAt: '2026-01-01T00:00:00Z',
      updatedAt: '2026-01-01T00:00:00Z',
    }
    mockGetSubscriptions.mockResolvedValueOnce([]).mockResolvedValue([newSub])
    mockGetCategories.mockResolvedValue(mockCategories)
    mockCreateSubscription.mockResolvedValue(newSub)

    renderSubscriptionsPage()
    await screen.findByText('有効なサブスクはありません')

    fireEvent.click(screen.getByRole('button', { name: '+ 追加' }))

    fireEvent.change(screen.getByRole('combobox', { name: 'カテゴリ' }), {
      target: { value: 'cat-1' },
    })
    fireEvent.change(screen.getByRole('textbox', { name: 'サービス名' }), {
      target: { value: 'Spotify' },
    })
    fireEvent.change(screen.getByRole('spinbutton', { name: '金額' }), {
      target: { value: '980' },
    })

    fireEvent.submit(screen.getByRole('button', { name: '登録' }).closest('form')!)

    await waitFor(() => {
      expect(mockCreateSubscription).toHaveBeenCalledWith(
        { categoryId: 'cat-1', name: 'Spotify', amount: 980 },
        expect.anything(),
      )
    })
    expect(await screen.findByText('サブスクを追加しました')).toBeInTheDocument()
  })

  it('登録失敗時はエラートーストを表示する', async () => {
    mockGetSubscriptions.mockResolvedValue([])
    mockGetCategories.mockResolvedValue(mockCategories)
    mockCreateSubscription.mockRejectedValue(new ApiError(400, { title: 'Bad Request' }))

    renderSubscriptionsPage()
    await screen.findByText('有効なサブスクはありません')

    fireEvent.click(screen.getByRole('button', { name: '+ 追加' }))
    fireEvent.change(screen.getByRole('combobox', { name: 'カテゴリ' }), {
      target: { value: 'cat-1' },
    })
    fireEvent.change(screen.getByRole('textbox', { name: 'サービス名' }), {
      target: { value: 'Spotify' },
    })
    fireEvent.change(screen.getByRole('spinbutton', { name: '金額' }), {
      target: { value: '980' },
    })
    fireEvent.submit(screen.getByRole('button', { name: '登録' }).closest('form')!)

    expect(await screen.findByText('サブスクの追加に失敗しました')).toBeInTheDocument()
  })

  it('編集ボタンを押すとモーダルに既存値がセットされる', async () => {
    mockGetSubscriptions.mockResolvedValue(mockSubscriptions)
    mockGetCategories.mockResolvedValue(mockCategories)

    renderSubscriptionsPage()
    await screen.findByRole('cell', { name: 'Netflix' })

    fireEvent.click(screen.getByRole('button', { name: '編集' }))

    expect(screen.getByRole('dialog', { name: 'サブスク編集' })).toBeInTheDocument()
    expect(screen.getByRole('textbox', { name: 'サービス名' })).toHaveValue('Netflix')
    expect(screen.getByRole('spinbutton', { name: '金額' })).toHaveValue(1490)
    expect(screen.getByRole('combobox', { name: 'カテゴリ' })).toHaveValue('cat-1')
  })

  it('カテゴリの種別が後からIncomeに変更されていても、編集モーダルの選択肢にそのカテゴリが残る', async () => {
    const legacySub: Subscription = {
      id: 'sub-legacy',
      categoryId: 'cat-2', // 給与(Income)。サブスク登録当時はExpenseだったが後で種別変更された想定
      name: 'Legacy Service',
      amount: 500,
      isActive: true,
      createdAt: '2026-01-01T00:00:00Z',
      updatedAt: '2026-01-01T00:00:00Z',
    }
    mockGetSubscriptions.mockResolvedValue([legacySub])
    mockGetCategories.mockResolvedValue(mockCategories)

    renderSubscriptionsPage()
    await screen.findByRole('cell', { name: 'Legacy Service' })

    fireEvent.click(screen.getByRole('button', { name: '編集' }))

    const categorySelect = screen.getByRole('combobox', { name: 'カテゴリ' })
    expect(categorySelect).toHaveValue('cat-2')
    expect(within(categorySelect).getByText('給与')).toBeInTheDocument()
  })

  it('更新成功 → isActive を維持したまま updateSubscription が呼ばれ成功トーストを表示する', async () => {
    const updated: Subscription = { ...mockSubscriptions[0]!, amount: 1590 }
    mockGetSubscriptions.mockResolvedValue(mockSubscriptions)
    mockGetCategories.mockResolvedValue(mockCategories)
    mockUpdateSubscription.mockResolvedValue(updated)

    renderSubscriptionsPage()
    await screen.findByRole('cell', { name: 'Netflix' })
    fireEvent.click(screen.getByRole('button', { name: '編集' }))

    fireEvent.change(screen.getByRole('spinbutton', { name: '金額' }), {
      target: { value: '1590' },
    })
    fireEvent.submit(screen.getByRole('button', { name: '更新' }).closest('form')!)

    await waitFor(() => {
      expect(mockUpdateSubscription).toHaveBeenCalledWith('sub-1', {
        categoryId: 'cat-1',
        name: 'Netflix',
        amount: 1590,
        isActive: true,
      })
    })
    expect(await screen.findByText('サブスクを更新しました')).toBeInTheDocument()
  })

  it('停止ボタンを押すと isActive: false で updateSubscription が呼ばれる', async () => {
    mockGetSubscriptions.mockResolvedValue(mockSubscriptions)
    mockGetCategories.mockResolvedValue(mockCategories)
    mockUpdateSubscription.mockResolvedValue({ ...mockSubscriptions[0]!, isActive: false })

    renderSubscriptionsPage()
    await screen.findByRole('cell', { name: 'Netflix' })

    fireEvent.click(screen.getByRole('button', { name: '停止' }))

    await waitFor(() => {
      expect(mockUpdateSubscription).toHaveBeenCalledWith('sub-1', {
        categoryId: 'cat-1',
        name: 'Netflix',
        amount: 1490,
        isActive: false,
      })
    })
    expect(await screen.findByText('サブスクを停止しました')).toBeInTheDocument()
  })

  it('再開ボタンを押すと isActive: true で updateSubscription が呼ばれる', async () => {
    mockGetSubscriptions.mockResolvedValue(mockSubscriptions)
    mockGetCategories.mockResolvedValue(mockCategories)
    mockUpdateSubscription.mockResolvedValue({ ...mockSubscriptions[1]!, isActive: true })

    renderSubscriptionsPage()
    await screen.findByRole('cell', { name: 'Disney+' })

    fireEvent.click(screen.getByRole('button', { name: '再開' }))

    await waitFor(() => {
      expect(mockUpdateSubscription).toHaveBeenCalledWith('sub-2', {
        categoryId: 'cat-1',
        name: 'Disney+',
        amount: 990,
        isActive: true,
      })
    })
    expect(await screen.findByText('サブスクを再開しました')).toBeInTheDocument()
  })

  it('削除ボタンを押し confirm=true → deleteSubscription が呼ばれ成功トーストを表示する', async () => {
    mockGetSubscriptions
      .mockResolvedValueOnce(mockSubscriptions)
      .mockResolvedValue([mockSubscriptions[0]!])
    mockGetCategories.mockResolvedValue(mockCategories)
    mockDeleteSubscription.mockResolvedValue(undefined)
    vi.spyOn(window, 'confirm').mockReturnValue(true)

    renderSubscriptionsPage()
    await screen.findByRole('cell', { name: 'Disney+' })

    fireEvent.click(screen.getByRole('button', { name: '削除' }))

    await waitFor(() => {
      expect(mockDeleteSubscription).toHaveBeenCalledWith('sub-2', expect.anything())
    })
    expect(await screen.findByText('サブスクを削除しました')).toBeInTheDocument()
  })

  it('削除ボタンを押し confirm=false → deleteSubscription が呼ばれない', async () => {
    mockGetSubscriptions.mockResolvedValue(mockSubscriptions)
    mockGetCategories.mockResolvedValue(mockCategories)
    vi.spyOn(window, 'confirm').mockReturnValue(false)

    renderSubscriptionsPage()
    await screen.findByRole('cell', { name: 'Disney+' })

    fireEvent.click(screen.getByRole('button', { name: '削除' }))

    expect(mockDeleteSubscription).not.toHaveBeenCalled()
  })

  it('削除失敗時はエラートーストを表示する', async () => {
    mockGetSubscriptions.mockResolvedValue(mockSubscriptions)
    mockGetCategories.mockResolvedValue(mockCategories)
    mockDeleteSubscription.mockRejectedValue(new ApiError(404, { title: 'Not Found' }))
    vi.spyOn(window, 'confirm').mockReturnValue(true)

    renderSubscriptionsPage()
    await screen.findByRole('cell', { name: 'Disney+' })

    fireEvent.click(screen.getByRole('button', { name: '削除' }))

    expect(await screen.findByText('サブスクの削除に失敗しました')).toBeInTheDocument()
  })
})
