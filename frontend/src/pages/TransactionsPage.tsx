import { useState, useEffect, type FormEvent } from 'react'
import { useTransactions, useCreateTransaction, useUpdateTransaction, useDeleteTransaction } from '../hooks/useTransactions'
import { useCategories } from '../hooks/useCategories'
import type { CategoryType } from '../api/categories'
import Header from '../components/Header'

function TransactionsPage() {
  const now = new Date()
  const [year, setYear] = useState(now.getFullYear())
  const [month, setMonth] = useState(now.getMonth() + 1)

  // client-side フィルタ
  const [typeFilter, setTypeFilter] = useState<CategoryType | 'All'>('All')
  const [categoryFilter, setCategoryFilter] = useState<string>('')

  // モーダル
  const [modalMode, setModalMode] = useState<null | 'create' | 'edit'>(null)
  const [editingId, setEditingId] = useState<string | null>(null)

  // フォーム（モーダル内共有）
  const [formType, setFormType] = useState<CategoryType>('Expense')
  const [formCategoryId, setFormCategoryId] = useState('')
  const [formAmount, setFormAmount] = useState('')
  const [formDate, setFormDate] = useState('')
  const [formMemo, setFormMemo] = useState('')

  const [toastMessage, setToastMessage] = useState<string | null>(null)

  const { data: transactions, isLoading, isError } = useTransactions(year, month)
  const { data: categories } = useCategories()
  const createMutation = useCreateTransaction()
  const updateMutation = useUpdateTransaction()
  const deleteMutation = useDeleteTransaction()

  useEffect(() => {
    if (isError) setToastMessage('データの取得に失敗しました')
  }, [isError])

  useEffect(() => {
    if (createMutation.isError) setToastMessage('収支の追加に失敗しました')
  }, [createMutation.isError])

  useEffect(() => {
    if (updateMutation.isError) setToastMessage('収支の更新に失敗しました')
  }, [updateMutation.isError])

  useEffect(() => {
    if (deleteMutation.isError) setToastMessage('収支の削除に失敗しました')
  }, [deleteMutation.isError])

  useEffect(() => {
    if (!toastMessage) return
    const id = setTimeout(() => setToastMessage(null), 3000)
    return () => clearTimeout(id)
  }, [toastMessage])

  function goToPrevMonth() {
    if (month === 1) {
      setYear((y) => y - 1)
      setMonth(12)
    } else {
      setMonth((m) => m - 1)
    }
  }

  function goToNextMonth() {
    if (month === 12) {
      setYear((y) => y + 1)
      setMonth(1)
    } else {
      setMonth((m) => m + 1)
    }
  }

  function handleTypeFilterChange(type: CategoryType | 'All') {
    setTypeFilter(type)
    setCategoryFilter('')
  }

  function openCreateModal() {
    setModalMode('create')
    setEditingId(null)
    setFormType('Expense')
    setFormCategoryId('')
    setFormAmount('')
    setFormDate('')
    setFormMemo('')
  }

  function openEditModal(id: string) {
    const tx = transactions?.find((t) => t.id === id)
    if (!tx) return
    const cat = categories?.find((c) => c.id === tx.categoryId)
    setModalMode('edit')
    setEditingId(id)
    setFormType(cat?.type ?? 'Expense')
    setFormCategoryId(tx.categoryId)
    setFormAmount(String(tx.amount))
    setFormDate(tx.date)
    setFormMemo(tx.memo ?? '')
  }

  function closeModal() {
    setModalMode(null)
    setEditingId(null)
  }

  function handleSubmit(e: FormEvent) {
    e.preventDefault()
    if (!formCategoryId || !formAmount || !formDate) return
    const body = {
      categoryId: formCategoryId,
      amount: Number(formAmount),
      date: formDate,
      memo: formMemo || null,
    }
    if (modalMode === 'create') {
      createMutation.mutate(body, {
        onSuccess: () => {
          closeModal()
          setToastMessage('収支を追加しました')
        },
      })
    } else if (modalMode === 'edit' && editingId) {
      updateMutation.mutate(
        { id: editingId, body },
        {
          onSuccess: () => {
            closeModal()
            setToastMessage('収支を更新しました')
          },
        },
      )
    }
  }

  function handleDelete(id: string) {
    if (!window.confirm('この収支を削除しますか？')) return
    deleteMutation.mutate(id, {
      onSuccess: () => setToastMessage('収支を削除しました'),
    })
  }

  // client-side フィルタ処理
  const filtered = (transactions ?? []).filter((tx) => {
    const cat = categories?.find((c) => c.id === tx.categoryId)
    if (typeFilter !== 'All' && cat?.type !== typeFilter) return false
    if (categoryFilter && tx.categoryId !== categoryFilter) return false
    return true
  })

  // フォームのカテゴリドロップダウン用（種別で絞り込み済み）
  const formCategories = (categories ?? []).filter((c) => c.type === formType)

  // フィルタ用カテゴリ（種別フィルタで絞り込み済み）
  const filterCategories =
    typeFilter === 'All' ? (categories ?? []) : (categories ?? []).filter((c) => c.type === typeFilter)

  const inputClass = 'w-full px-3 py-2 border border-gray-300 rounded-lg focus:outline-none focus:ring-2 focus:ring-blue-500 focus:border-transparent text-sm'
  const selectClass = inputClass
  const labelClass = 'block text-sm font-medium text-gray-700 mb-1'

  return (
    <div className="min-h-screen">
      <Header />

      <main className="mx-auto max-w-7xl px-4 py-6">
        <div className="flex flex-wrap items-center gap-4 mb-6">
          {/* 月ナビゲーション */}
          <div className="flex items-center gap-2">
            <button
              type="button"
              onClick={goToPrevMonth}
              className="px-3 py-1.5 bg-white border border-gray-300 rounded-lg hover:bg-gray-50 transition-colors"
            >
              ◄
            </button>
            <span className="text-lg font-semibold text-gray-900 min-w-28 text-center">
              {year}年{month}月
            </span>
            <button
              type="button"
              onClick={goToNextMonth}
              className="px-3 py-1.5 bg-white border border-gray-300 rounded-lg hover:bg-gray-50 transition-colors"
            >
              ►
            </button>
          </div>

          <button
            type="button"
            onClick={openCreateModal}
            className="ml-auto px-4 py-2 bg-blue-600 hover:bg-blue-700 text-white text-sm font-medium rounded-lg transition-colors"
          >
            + 新規登録
          </button>
        </div>

        {/* フィルター */}
        <div className="flex flex-wrap items-center gap-4 mb-4">
          <div className="flex items-center gap-2">
            <label htmlFor="type-filter" className="text-sm font-medium text-gray-700">
              種別
            </label>
            <select
              id="type-filter"
              value={typeFilter}
              onChange={(e) => handleTypeFilterChange(e.target.value as CategoryType | 'All')}
              aria-label="種別フィルタ"
              className="px-3 py-1.5 border border-gray-300 rounded-lg text-sm focus:outline-none focus:ring-2 focus:ring-blue-500"
            >
              <option value="All">すべて</option>
              <option value="Expense">支出</option>
              <option value="Income">収入</option>
            </select>
          </div>

          <div className="flex items-center gap-2">
            <label htmlFor="category-filter" className="text-sm font-medium text-gray-700">
              カテゴリ
            </label>
            <select
              id="category-filter"
              value={categoryFilter}
              onChange={(e) => setCategoryFilter(e.target.value)}
              aria-label="カテゴリフィルタ"
              className="px-3 py-1.5 border border-gray-300 rounded-lg text-sm focus:outline-none focus:ring-2 focus:ring-blue-500"
            >
              <option value="">すべて</option>
              {filterCategories.map((c) => (
                <option key={c.id} value={c.id}>
                  {c.name}
                </option>
              ))}
            </select>
          </div>
        </div>

        {isLoading && (
          <div role="status" aria-label="読み込み中" className="text-gray-500 py-8 text-center">
            読み込み中...
          </div>
        )}

        {transactions && (
          <div className="bg-white rounded-xl shadow-sm border border-gray-100 overflow-hidden">
            <table className="w-full text-sm">
              <thead className="bg-gray-50">
                <tr>
                  <th className="px-5 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">
                    日付
                  </th>
                  <th className="px-5 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">
                    カテゴリ
                  </th>
                  <th className="px-5 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">
                    内容
                  </th>
                  <th className="px-5 py-3 text-right text-xs font-medium text-gray-500 uppercase tracking-wider">
                    金額
                  </th>
                  <th className="px-5 py-3 text-center text-xs font-medium text-gray-500 uppercase tracking-wider">
                    操作
                  </th>
                </tr>
              </thead>
              <tbody className="divide-y divide-gray-100">
                {filtered.length === 0 ? (
                  <tr>
                    <td colSpan={5} className="px-5 py-8 text-center text-gray-400">
                      収支なし
                    </td>
                  </tr>
                ) : (
                  filtered.map((tx) => {
                    const cat = categories?.find((c) => c.id === tx.categoryId)
                    const isIncome = cat?.type === 'Income'
                    const amountDisplay = isIncome
                      ? `+¥${tx.amount.toLocaleString()}`
                      : `-¥${tx.amount.toLocaleString()}`
                    return (
                      <tr key={tx.id} className="hover:bg-gray-50">
                        <td className="px-5 py-3 text-gray-600">{tx.date}</td>
                        <td className="px-5 py-3 text-gray-900">{tx.categoryName}</td>
                        <td className="px-5 py-3 text-gray-500">{tx.memo ?? '—'}</td>
                        <td
                          className={`px-5 py-3 text-right font-medium ${isIncome ? 'text-green-600' : 'text-red-600'}`}
                        >
                          {amountDisplay}
                        </td>
                        <td className="px-5 py-3 text-center">
                          <button
                            type="button"
                            onClick={() => openEditModal(tx.id)}
                            className="px-2.5 py-1 text-xs bg-gray-100 hover:bg-gray-200 text-gray-700 rounded transition-colors mr-1"
                          >
                            編集
                          </button>
                          <button
                            type="button"
                            onClick={() => handleDelete(tx.id)}
                            disabled={deleteMutation.isPending}
                            className="px-2.5 py-1 text-xs bg-red-50 hover:bg-red-100 text-red-600 rounded transition-colors disabled:opacity-50"
                          >
                            削除
                          </button>
                        </td>
                      </tr>
                    )
                  })
                )}
              </tbody>
            </table>
          </div>
        )}
      </main>

      {/* モーダル */}
      {modalMode !== null && (
        <div
          role="dialog"
          aria-modal="true"
          aria-label={modalMode === 'create' ? '新規登録' : '収支編集'}
          className="fixed inset-0 bg-black/40 flex items-center justify-center px-4 z-50"
        >
          <div className="bg-white rounded-2xl shadow-xl w-full max-w-md p-6">
            <h3 className="text-lg font-semibold text-gray-900 mb-5">
              {modalMode === 'create' ? '新規登録' : '収支編集'}
            </h3>
            <form onSubmit={handleSubmit} className="space-y-4">
              <div>
                <label htmlFor="form-type" className={labelClass}>
                  種別
                </label>
                <select
                  id="form-type"
                  value={formType}
                  onChange={(e) => {
                    setFormType(e.target.value as CategoryType)
                    setFormCategoryId('')
                  }}
                  className={selectClass}
                >
                  <option value="Expense">支出</option>
                  <option value="Income">収入</option>
                </select>
              </div>
              <div>
                <label htmlFor="form-category" className={labelClass}>
                  カテゴリ
                </label>
                <select
                  id="form-category"
                  value={formCategoryId}
                  onChange={(e) => setFormCategoryId(e.target.value)}
                  required
                  className={selectClass}
                >
                  <option value="">選択してください</option>
                  {formCategories.map((c) => (
                    <option key={c.id} value={c.id}>
                      {c.name}
                    </option>
                  ))}
                </select>
              </div>
              <div>
                <label htmlFor="form-amount" className={labelClass}>
                  金額
                </label>
                <input
                  id="form-amount"
                  type="number"
                  min="1"
                  value={formAmount}
                  onChange={(e) => setFormAmount(e.target.value)}
                  required
                  className={inputClass}
                />
              </div>
              <div>
                <label htmlFor="form-date" className={labelClass}>
                  日付
                </label>
                <input
                  id="form-date"
                  type="date"
                  value={formDate}
                  onChange={(e) => setFormDate(e.target.value)}
                  required
                  className={inputClass}
                />
              </div>
              <div>
                <label htmlFor="form-memo" className={labelClass}>
                  メモ
                </label>
                <input
                  id="form-memo"
                  type="text"
                  value={formMemo}
                  onChange={(e) => setFormMemo(e.target.value)}
                  className={inputClass}
                />
              </div>
              <div className="flex gap-3 pt-2">
                <button
                  type="submit"
                  disabled={createMutation.isPending || updateMutation.isPending}
                  className="flex-1 py-2 bg-blue-600 hover:bg-blue-700 disabled:bg-blue-300 text-white text-sm font-medium rounded-lg transition-colors"
                >
                  {modalMode === 'create' ? '登録' : '更新'}
                </button>
                <button
                  type="button"
                  onClick={closeModal}
                  className="flex-1 py-2 bg-gray-100 hover:bg-gray-200 text-gray-700 text-sm font-medium rounded-lg transition-colors"
                >
                  キャンセル
                </button>
              </div>
            </form>
          </div>
        </div>
      )}

      {toastMessage && (
        <div
          role="alert"
          className="fixed bottom-4 right-4 bg-gray-800 text-white px-4 py-3 rounded-lg shadow-lg text-sm"
        >
          {toastMessage}
        </div>
      )}
    </div>
  )
}

export default TransactionsPage
