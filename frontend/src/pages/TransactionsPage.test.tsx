import { describe, it, expect, vi, beforeEach, afterEach } from 'vitest'
import { render, screen, fireEvent, waitFor } from '@testing-library/react'
import { QueryClient, QueryClientProvider } from '@tanstack/react-query'
import { MemoryRouter, Route, Routes } from 'react-router-dom'
import TransactionsPage from './TransactionsPage'
import { getTransactions, createTransaction, updateTransaction, deleteTransaction } from '../api/transactions'
import { getCategories } from '../api/categories'
import { logout, refresh } from '../api/auth'
import { ApiError } from '../api/client'
import type { Transaction } from '../api/transactions'
import type { Category } from '../api/categories'

vi.mock('../api/transactions')
vi.mock('../api/categories')
vi.mock('../api/auth')

const mockGetTransactions = vi.mocked(getTransactions)
const mockCreateTransaction = vi.mocked(createTransaction)
const mockUpdateTransaction = vi.mocked(updateTransaction)
const mockDeleteTransaction = vi.mocked(deleteTransaction)
const mockGetCategories = vi.mocked(getCategories)
const mockRefresh = vi.mocked(refresh)
const mockLogout = vi.mocked(logout)

const mockCategories: Category[] = [
  { id: 'cat-1', name: '食費', type: 'Expense', createdAt: '2026-01-01T00:00:00Z' },
  { id: 'cat-2', name: '給与', type: 'Income', createdAt: '2026-01-01T00:00:00Z' },
]

const mockTransactions: Transaction[] = [
  {
    id: 'tx-1',
    categoryId: 'cat-1',
    subscriptionId: null,
    amount: 3000,
    date: '2026-07-10',
    memo: '昼食',
    receiptS3Key: null,
    createdAt: '2026-07-10T00:00:00Z',
    updatedAt: '2026-07-10T00:00:00Z',
  },
  {
    id: 'tx-2',
    categoryId: 'cat-2',
    subscriptionId: null,
    amount: 200000,
    date: '2026-07-25',
    memo: null,
    receiptS3Key: null,
    createdAt: '2026-07-25T00:00:00Z',
    updatedAt: '2026-07-25T00:00:00Z',
  },
]

function renderTransactionsPage() {
  const queryClient = new QueryClient({
    defaultOptions: { queries: { retry: false } },
  })
  return render(
    <QueryClientProvider client={queryClient}>
      <MemoryRouter initialEntries={['/transactions']}>
        <Routes>
          <Route path="/transactions" element={<TransactionsPage />} />
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

describe('TransactionsPage', () => {
  it('収支一覧を表示する（カテゴリ名と金額が正しく表示される）', async () => {
    mockGetTransactions.mockResolvedValue(mockTransactions)
    mockGetCategories.mockResolvedValue(mockCategories)

    renderTransactionsPage()

    // <td>食費</td> が表示されるまで待つ
    expect(await screen.findByRole('cell', { name: '食費' })).toBeInTheDocument()
    expect(screen.getByText('-¥3,000')).toBeInTheDocument()
    expect(screen.getByRole('cell', { name: '給与' })).toBeInTheDocument()
    expect(screen.getByText('+¥200,000')).toBeInTheDocument()
    expect(screen.getByRole('cell', { name: '昼食' })).toBeInTheDocument()
  })

  it('収支が0件のとき「収支なし」を表示する', async () => {
    mockGetTransactions.mockResolvedValue([])
    mockGetCategories.mockResolvedValue(mockCategories)

    renderTransactionsPage()

    expect(await screen.findByText('収支なし')).toBeInTheDocument()
  })

  it('読み込み中はスピナーを表示し、完了後は消える', async () => {
    let resolve: (value: Transaction[]) => void = () => {}
    mockGetTransactions.mockReturnValue(new Promise((r) => { resolve = r }))
    mockGetCategories.mockResolvedValue(mockCategories)

    renderTransactionsPage()

    expect(screen.getByRole('status')).toBeInTheDocument()

    resolve(mockTransactions)
    await screen.findByRole('cell', { name: '食費' })

    expect(screen.queryByRole('status')).not.toBeInTheDocument()
  })

  it('401 → refresh 成功 → 一覧表示する', async () => {
    mockGetTransactions
      .mockRejectedValueOnce(new ApiError(401, {}))
      .mockResolvedValueOnce(mockTransactions)
    mockRefresh.mockResolvedValue({ accessTokenExpiresAt: '2026-07-28T00:00:00Z' })
    mockGetCategories.mockResolvedValue(mockCategories)

    renderTransactionsPage()

    expect(await screen.findByRole('cell', { name: '食費' })).toBeInTheDocument()
    expect(mockRefresh).toHaveBeenCalledTimes(1)
  })

  it('401 → refresh 失敗 → /login へリダイレクト', async () => {
    mockGetTransactions.mockRejectedValue(new ApiError(401, {}))
    mockRefresh.mockRejectedValue(new ApiError(401, {}))
    mockGetCategories.mockResolvedValue(mockCategories)

    renderTransactionsPage()

    expect(await screen.findByText('Login')).toBeInTheDocument()
  })

  it('非401エラー時はトーストを表示する', async () => {
    mockGetTransactions.mockRejectedValue(new ApiError(500, { title: 'Server Error' }))
    mockGetCategories.mockResolvedValue(mockCategories)

    renderTransactionsPage()

    expect(await screen.findByText('データの取得に失敗しました')).toBeInTheDocument()
    expect(screen.queryByText('Login')).not.toBeInTheDocument()
  })

  it('1月に「前月」を押すと前年12月に切り替わる（境界値）', async () => {
    mockGetTransactions.mockResolvedValue([])
    mockGetCategories.mockResolvedValue([])

    renderTransactionsPage()
    await screen.findByText('収支なし')

    const prevButton = screen.getByRole('button', { name: '◄' })
    const now = new Date()
    const currentMonth = now.getMonth() + 1
    const currentYear = now.getFullYear()

    // 1月になるまで前月ボタンを押す（currentMonth - 1 回）
    for (let i = 0; i < currentMonth - 1; i++) {
      fireEvent.click(prevButton)
    }

    // 現在 currentYear年1月。前月ボタンを押すと前年12月になる
    fireEvent.click(prevButton)

    await waitFor(() => {
      expect(screen.getByText(`${currentYear - 1}年12月`)).toBeInTheDocument()
    })
  })

  it('12月に「次月」を押すと翌年1月に切り替わる（境界値）', async () => {
    mockGetTransactions.mockResolvedValue([])
    mockGetCategories.mockResolvedValue([])

    renderTransactionsPage()
    await screen.findByText('収支なし')

    const nextButton = screen.getByRole('button', { name: '►' })
    const now = new Date()
    const currentMonth = now.getMonth() + 1
    const currentYear = now.getFullYear()

    // 12月になるまで次月ボタンを押す（12 - currentMonth 回）
    for (let i = 0; i < 12 - currentMonth; i++) {
      fireEvent.click(nextButton)
    }

    // 現在 currentYear年12月。次月ボタンを押すと翌年1月になる
    fireEvent.click(nextButton)

    await waitFor(() => {
      expect(screen.getByText(`${currentYear + 1}年1月`)).toBeInTheDocument()
    })
  })

  it('種別フィルタを「支出」に変えると支出カテゴリの収支のみ表示される', async () => {
    mockGetTransactions.mockResolvedValue(mockTransactions)
    mockGetCategories.mockResolvedValue(mockCategories)

    renderTransactionsPage()
    await screen.findByRole('cell', { name: '食費' })

    fireEvent.change(screen.getByRole('combobox', { name: '種別フィルタ' }), {
      target: { value: 'Expense' },
    })

    expect(screen.getByRole('cell', { name: '食費' })).toBeInTheDocument()
    expect(screen.queryByRole('cell', { name: '給与' })).not.toBeInTheDocument()
  })

  it('カテゴリフィルタで絞り込むと該当カテゴリの収支のみ表示される', async () => {
    mockGetTransactions.mockResolvedValue(mockTransactions)
    mockGetCategories.mockResolvedValue(mockCategories)

    renderTransactionsPage()
    await screen.findByRole('cell', { name: '食費' })

    fireEvent.change(screen.getByRole('combobox', { name: 'カテゴリフィルタ' }), {
      target: { value: 'cat-1' },
    })

    expect(screen.getByRole('cell', { name: '食費' })).toBeInTheDocument()
    expect(screen.queryByRole('cell', { name: '給与' })).not.toBeInTheDocument()
  })

  it('種別フィルタ変更時にカテゴリフィルタがリセットされる', async () => {
    mockGetTransactions.mockResolvedValue(mockTransactions)
    mockGetCategories.mockResolvedValue(mockCategories)

    renderTransactionsPage()
    await screen.findByRole('cell', { name: '食費' })

    // カテゴリフィルタをセット
    fireEvent.change(screen.getByRole('combobox', { name: 'カテゴリフィルタ' }), {
      target: { value: 'cat-1' },
    })
    expect(screen.getByRole('combobox', { name: 'カテゴリフィルタ' })).toHaveValue('cat-1')

    // 種別フィルタを変更
    fireEvent.change(screen.getByRole('combobox', { name: '種別フィルタ' }), {
      target: { value: 'Income' },
    })

    // カテゴリフィルタがリセットされる
    expect(screen.getByRole('combobox', { name: 'カテゴリフィルタ' })).toHaveValue('')
  })

  it('新規登録ボタンを押すとモーダルが表示される', async () => {
    mockGetTransactions.mockResolvedValue([])
    mockGetCategories.mockResolvedValue(mockCategories)

    renderTransactionsPage()
    await screen.findByText('収支なし')

    fireEvent.click(screen.getByRole('button', { name: '+ 新規登録' }))

    expect(screen.getByRole('dialog', { name: '新規登録' })).toBeInTheDocument()
  })

  it('新規登録フォーム送信 → createTransaction が呼ばれ成功トーストを表示する', async () => {
    const newTx: Transaction = {
      id: 'tx-3',
      categoryId: 'cat-1',
      subscriptionId: null,
      amount: 500,
      date: '2026-07-15',
      memo: 'コーヒー',
      receiptS3Key: null,
      createdAt: '2026-07-15T00:00:00Z',
      updatedAt: '2026-07-15T00:00:00Z',
    }
    mockGetTransactions
      .mockResolvedValueOnce(mockTransactions)
      .mockResolvedValue([...mockTransactions, newTx])
    mockGetCategories.mockResolvedValue(mockCategories)
    mockCreateTransaction.mockResolvedValue(newTx)

    renderTransactionsPage()
    await screen.findByRole('cell', { name: '食費' })

    fireEvent.click(screen.getByRole('button', { name: '+ 新規登録' }))

    // 種別は既定で「支出」
    fireEvent.change(screen.getByRole('combobox', { name: 'カテゴリ' }), {
      target: { value: 'cat-1' },
    })
    fireEvent.change(screen.getByRole('spinbutton', { name: '金額' }), {
      target: { value: '500' },
    })
    // type="date" は textbox ロールを持たないため getByLabelText を使う
    fireEvent.change(screen.getByLabelText('日付'), {
      target: { value: '2026-07-15' },
    })
    fireEvent.change(screen.getByRole('textbox', { name: 'メモ' }), {
      target: { value: 'コーヒー' },
    })

    fireEvent.submit(screen.getByRole('button', { name: '登録' }).closest('form')!)

    await waitFor(() => {
      expect(mockCreateTransaction).toHaveBeenCalledWith(
        { categoryId: 'cat-1', amount: 500, date: '2026-07-15', memo: 'コーヒー' },
        expect.anything(),
      )
    })
    expect(await screen.findByText('収支を追加しました')).toBeInTheDocument()
  })

  it('登録失敗時はエラートーストを表示する', async () => {
    mockGetTransactions.mockResolvedValue(mockTransactions)
    mockGetCategories.mockResolvedValue(mockCategories)
    mockCreateTransaction.mockRejectedValue(new ApiError(400, { title: 'Bad Request' }))

    renderTransactionsPage()
    await screen.findByRole('cell', { name: '食費' })

    fireEvent.click(screen.getByRole('button', { name: '+ 新規登録' }))

    fireEvent.change(screen.getByRole('combobox', { name: 'カテゴリ' }), {
      target: { value: 'cat-1' },
    })
    fireEvent.change(screen.getByRole('spinbutton', { name: '金額' }), {
      target: { value: '500' },
    })
    fireEvent.change(screen.getByLabelText('日付'), {
      target: { value: '2026-07-15' },
    })

    fireEvent.submit(screen.getByRole('button', { name: '登録' }).closest('form')!)

    expect(await screen.findByText('収支の追加に失敗しました')).toBeInTheDocument()
  })

  it('編集ボタンを押すとモーダルに既存値がセットされる', async () => {
    mockGetTransactions.mockResolvedValue(mockTransactions)
    mockGetCategories.mockResolvedValue(mockCategories)

    renderTransactionsPage()
    const editButtons = await screen.findAllByRole('button', { name: '編集' })

    fireEvent.click(editButtons[0]!)

    expect(screen.getByRole('dialog', { name: '収支編集' })).toBeInTheDocument()
    expect(screen.getByRole('spinbutton', { name: '金額' })).toHaveValue(3000)
    // type="date" は textbox ロールを持たないため getByLabelText を使う
    expect(screen.getByLabelText('日付')).toHaveValue('2026-07-10')
    expect(screen.getByRole('textbox', { name: 'メモ' })).toHaveValue('昼食')
  })

  it('更新成功 → 成功トーストを表示する', async () => {
    const updated: Transaction = { ...mockTransactions[0]!, amount: 4000 }
    mockGetTransactions.mockResolvedValue(mockTransactions)
    mockGetCategories.mockResolvedValue(mockCategories)
    mockUpdateTransaction.mockResolvedValue(updated)

    renderTransactionsPage()
    const editButtons = await screen.findAllByRole('button', { name: '編集' })
    fireEvent.click(editButtons[0]!)

    fireEvent.change(screen.getByRole('spinbutton', { name: '金額' }), {
      target: { value: '4000' },
    })
    fireEvent.submit(screen.getByRole('button', { name: '更新' }).closest('form')!)

    await waitFor(() => {
      expect(mockUpdateTransaction).toHaveBeenCalledWith(
        'tx-1',
        { categoryId: 'cat-1', amount: 4000, date: '2026-07-10', memo: '昼食' },
      )
    })
    expect(await screen.findByText('収支を更新しました')).toBeInTheDocument()
  })

  it('削除ボタンを押し confirm=true → deleteTransaction が呼ばれ成功トーストを表示する', async () => {
    mockGetTransactions
      .mockResolvedValueOnce(mockTransactions)
      .mockResolvedValue([mockTransactions[1]!])
    mockGetCategories.mockResolvedValue(mockCategories)
    mockDeleteTransaction.mockResolvedValue(undefined)
    vi.spyOn(window, 'confirm').mockReturnValue(true)

    renderTransactionsPage()
    const deleteButtons = await screen.findAllByRole('button', { name: '削除' })

    fireEvent.click(deleteButtons[0]!)

    await waitFor(() => {
      expect(mockDeleteTransaction).toHaveBeenCalledWith('tx-1', expect.anything())
    })
    expect(await screen.findByText('収支を削除しました')).toBeInTheDocument()
  })

  it('削除ボタンを押し confirm=false → deleteTransaction が呼ばれない', async () => {
    mockGetTransactions.mockResolvedValue(mockTransactions)
    mockGetCategories.mockResolvedValue(mockCategories)
    vi.spyOn(window, 'confirm').mockReturnValue(false)

    renderTransactionsPage()
    const deleteButtons = await screen.findAllByRole('button', { name: '削除' })

    fireEvent.click(deleteButtons[0]!)

    expect(mockDeleteTransaction).not.toHaveBeenCalled()
  })

  it('削除失敗時はエラートーストを表示する', async () => {
    mockGetTransactions.mockResolvedValue(mockTransactions)
    mockGetCategories.mockResolvedValue(mockCategories)
    mockDeleteTransaction.mockRejectedValue(new ApiError(404, { title: 'Not Found' }))
    vi.spyOn(window, 'confirm').mockReturnValue(true)

    renderTransactionsPage()
    const deleteButtons = await screen.findAllByRole('button', { name: '削除' })

    fireEvent.click(deleteButtons[0]!)

    expect(await screen.findByText('収支の削除に失敗しました')).toBeInTheDocument()
  })
})
