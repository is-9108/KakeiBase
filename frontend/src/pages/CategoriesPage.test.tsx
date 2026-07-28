import { describe, it, expect, vi, beforeEach } from 'vitest'
import { render, screen, fireEvent, waitFor } from '@testing-library/react'
import { QueryClient, QueryClientProvider } from '@tanstack/react-query'
import { MemoryRouter, Route, Routes } from 'react-router-dom'
import CategoriesPage from './CategoriesPage'
import { getCategories, createCategory, updateCategory, deleteCategory } from '../api/categories'
import { logout, refresh } from '../api/auth'
import { ApiError } from '../api/client'
import type { Category } from '../api/categories'

vi.mock('../api/categories')
vi.mock('../api/auth')

const mockGetCategories = vi.mocked(getCategories)
const mockCreateCategory = vi.mocked(createCategory)
const mockUpdateCategory = vi.mocked(updateCategory)
const mockDeleteCategory = vi.mocked(deleteCategory)
const mockRefresh = vi.mocked(refresh)
const mockLogout = vi.mocked(logout)

const mockCategories: Category[] = [
  { id: '1', name: '食費', type: 'Expense', createdAt: '2026-01-01T00:00:00Z' },
  { id: '2', name: '給与', type: 'Income', createdAt: '2026-01-01T00:00:00Z' },
]

function renderCategoriesPage() {
  const queryClient = new QueryClient({
    defaultOptions: { queries: { retry: false } },
  })
  return render(
    <QueryClientProvider client={queryClient}>
      <MemoryRouter initialEntries={['/categories']}>
        <Routes>
          <Route path="/categories" element={<CategoriesPage />} />
          <Route path="/login" element={<div>Login</div>} />
          <Route path="/" element={<div>Dashboard</div>} />
        </Routes>
      </MemoryRouter>
    </QueryClientProvider>,
  )
}

beforeEach(() => {
  vi.resetAllMocks()
})

describe('CategoriesPage', () => {
  it('カテゴリ一覧を表示する', async () => {
    mockGetCategories.mockResolvedValue(mockCategories)

    renderCategoriesPage()

    expect(await screen.findByText('食費')).toBeInTheDocument()
    expect(screen.getByText('給与')).toBeInTheDocument()
    // フォームの<option>と<td>の両方に「収入」が存在するためAllByを使う
    expect(screen.getAllByText('収入').length).toBeGreaterThan(0)
  })

  it('カテゴリが0件のときは「カテゴリなし」を表示する', async () => {
    mockGetCategories.mockResolvedValue([])

    renderCategoriesPage()

    expect(await screen.findByText('カテゴリなし')).toBeInTheDocument()
  })

  it('読み込み中はスピナーを表示し、完了後は消える', async () => {
    let resolve: (value: Category[]) => void = () => {}
    mockGetCategories.mockReturnValue(new Promise((r) => { resolve = r }))

    renderCategoriesPage()

    expect(screen.getByRole('status')).toBeInTheDocument()

    resolve(mockCategories)
    await screen.findByText('食費')

    expect(screen.queryByRole('status')).not.toBeInTheDocument()
  })

  it('401エラー後にrefresh成功したらカテゴリ一覧を表示する', async () => {
    mockGetCategories
      .mockRejectedValueOnce(new ApiError(401, {}))
      .mockResolvedValueOnce(mockCategories)
    mockRefresh.mockResolvedValue({ accessTokenExpiresAt: '2026-07-28T00:00:00Z' })

    renderCategoriesPage()

    expect(await screen.findByText('食費')).toBeInTheDocument()
    expect(mockRefresh).toHaveBeenCalledTimes(1)
  })

  it('401エラー後にrefreshも失敗したら/loginへリダイレクトする', async () => {
    mockGetCategories.mockRejectedValue(new ApiError(401, {}))
    mockRefresh.mockRejectedValue(new ApiError(401, {}))

    renderCategoriesPage()

    expect(await screen.findByText('Login')).toBeInTheDocument()
  })

  it('非401エラー時はトーストを表示し/loginへリダイレクトしない', async () => {
    mockGetCategories.mockRejectedValue(new ApiError(500, { title: 'Server Error' }))

    renderCategoriesPage()

    expect(await screen.findByText('データの取得に失敗しました')).toBeInTheDocument()
    expect(screen.queryByText('Login')).not.toBeInTheDocument()
  })

  describe('カテゴリ追加', () => {
    it('フォームを送信すると createCategory が呼ばれ、成功トーストを表示する', async () => {
      const newCategory: Category = { id: '3', name: '交通費', type: 'Expense', createdAt: '2026-01-01T00:00:00Z' }
      mockGetCategories
        .mockResolvedValueOnce(mockCategories)
        .mockResolvedValue([...mockCategories, newCategory])
      mockCreateCategory.mockResolvedValue(newCategory)

      renderCategoriesPage()
      await screen.findByText('食費')

      fireEvent.change(screen.getByRole('textbox', { name: 'カテゴリ名' }), {
        target: { value: '交通費' },
      })
      fireEvent.submit(screen.getByRole('button', { name: '追加' }).closest('form')!)

      await waitFor(() => {
        // React Query v5 は mutationFn の第2引数にコンテキストを渡すため expect.anything() で許容する
        expect(mockCreateCategory).toHaveBeenCalledWith({ name: '交通費', type: 'Expense' }, expect.anything())
      })
      expect(await screen.findByText('カテゴリを追加しました')).toBeInTheDocument()
    })

    it('追加失敗時はエラートーストを表示する', async () => {
      mockGetCategories.mockResolvedValue(mockCategories)
      mockCreateCategory.mockRejectedValue(new ApiError(409, { title: 'Conflict' }))

      renderCategoriesPage()
      await screen.findByText('食費')

      fireEvent.change(screen.getByRole('textbox', { name: 'カテゴリ名' }), {
        target: { value: '食費' },
      })
      fireEvent.submit(screen.getByRole('button', { name: '追加' }).closest('form')!)

      expect(await screen.findByText('カテゴリの追加に失敗しました')).toBeInTheDocument()
    })
  })

  describe('カテゴリ削除', () => {
    it('削除ボタンを押すと deleteCategory が呼ばれ、成功トーストを表示する', async () => {
      mockGetCategories
        .mockResolvedValueOnce(mockCategories)
        .mockResolvedValue([mockCategories[1]!])
      mockDeleteCategory.mockResolvedValue(undefined)

      renderCategoriesPage()
      const deleteButtons = await screen.findAllByRole('button', { name: '削除' })

      fireEvent.click(deleteButtons[0]!)

      await waitFor(() => {
        // React Query v5 は mutationFn の第2引数にコンテキストを渡すため expect.anything() で許容する
        expect(mockDeleteCategory).toHaveBeenCalledWith('1', expect.anything())
      })
      expect(await screen.findByText('カテゴリを削除しました')).toBeInTheDocument()
    })

    it('削除失敗時はエラートーストを表示する', async () => {
      mockGetCategories.mockResolvedValue(mockCategories)
      mockDeleteCategory.mockRejectedValue(new ApiError(404, { title: 'Not Found' }))

      renderCategoriesPage()
      const deleteButtons = await screen.findAllByRole('button', { name: '削除' })

      fireEvent.click(deleteButtons[0]!)

      expect(await screen.findByText('カテゴリの削除に失敗しました')).toBeInTheDocument()
    })
  })

  describe('カテゴリ編集', () => {
    it('編集ボタンを押すとインライン編集フォームが表示される', async () => {
      mockGetCategories.mockResolvedValue(mockCategories)

      renderCategoriesPage()
      const editButtons = await screen.findAllByRole('button', { name: '編集' })

      fireEvent.click(editButtons[0]!)

      expect(screen.getByRole('textbox', { name: 'カテゴリ名(編集)' })).toHaveValue('食費')
      expect(screen.getByRole('button', { name: '保存' })).toBeInTheDocument()
      expect(screen.getByRole('button', { name: 'キャンセル' })).toBeInTheDocument()
    })

    it('保存ボタンを押すと updateCategory が呼ばれ、成功トーストを表示する', async () => {
      mockGetCategories.mockResolvedValue(mockCategories)
      const updated: Category = { id: '1', name: '食費(外食)', type: 'Expense', createdAt: '2026-01-01T00:00:00Z' }
      mockUpdateCategory.mockResolvedValue(updated)

      renderCategoriesPage()
      const editButtons = await screen.findAllByRole('button', { name: '編集' })
      fireEvent.click(editButtons[0]!)

      const nameInput = screen.getByRole('textbox', { name: 'カテゴリ名(編集)' })
      fireEvent.change(nameInput, { target: { value: '食費(外食)' } })
      fireEvent.click(screen.getByRole('button', { name: '保存' }))

      await waitFor(() => {
        expect(mockUpdateCategory).toHaveBeenCalledWith('1', { name: '食費(外食)', type: 'Expense' })
      })
      expect(await screen.findByText('カテゴリを更新しました')).toBeInTheDocument()
    })

    it('キャンセルボタンを押すとインライン編集フォームが閉じる', async () => {
      mockGetCategories.mockResolvedValue(mockCategories)

      renderCategoriesPage()
      const editButtons = await screen.findAllByRole('button', { name: '編集' })
      fireEvent.click(editButtons[0]!)

      expect(screen.getByRole('button', { name: '保存' })).toBeInTheDocument()

      fireEvent.click(screen.getByRole('button', { name: 'キャンセル' }))

      expect(screen.queryByRole('button', { name: '保存' })).not.toBeInTheDocument()
    })
  })

  it('ダッシュボードボタンを押すと/へ遷移する', async () => {
    mockGetCategories.mockResolvedValue(mockCategories)

    renderCategoriesPage()
    await screen.findByText('食費')

    fireEvent.click(screen.getByRole('button', { name: 'ダッシュボード' }))

    expect(await screen.findByText('Dashboard')).toBeInTheDocument()
  })

  it('ログアウトボタンを押すと logout を呼び /login へ遷移する', async () => {
    mockGetCategories.mockResolvedValue(mockCategories)
    mockLogout.mockResolvedValue(undefined)

    renderCategoriesPage()
    await screen.findByText('食費')

    fireEvent.click(screen.getByRole('button', { name: 'ログアウト' }))

    expect(await screen.findByText('Login')).toBeInTheDocument()
    expect(mockLogout).toHaveBeenCalledTimes(1)
  })
})
