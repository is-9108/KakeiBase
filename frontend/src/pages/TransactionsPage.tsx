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

  return (
    <div>
      <Header />

      <h2>収支一覧</h2>

      <div>
        <button type="button" onClick={goToPrevMonth}>
          ◄
        </button>
        <span>
          {year}年{month}月
        </span>
        <button type="button" onClick={goToNextMonth}>
          ►
        </button>
        <button type="button" onClick={openCreateModal}>
          + 新規登録
        </button>
      </div>

      <div>
        <label htmlFor="type-filter">種別</label>
        <select
          id="type-filter"
          value={typeFilter}
          onChange={(e) => handleTypeFilterChange(e.target.value as CategoryType | 'All')}
          aria-label="種別フィルタ"
        >
          <option value="All">すべて</option>
          <option value="Expense">支出</option>
          <option value="Income">収入</option>
        </select>

        <label htmlFor="category-filter">カテゴリ</label>
        <select
          id="category-filter"
          value={categoryFilter}
          onChange={(e) => setCategoryFilter(e.target.value)}
          aria-label="カテゴリフィルタ"
        >
          <option value="">すべて</option>
          {filterCategories.map((c) => (
            <option key={c.id} value={c.id}>
              {c.name}
            </option>
          ))}
        </select>
      </div>

      {isLoading && (
        <div role="status" aria-label="読み込み中">
          読み込み中...
        </div>
      )}

      {transactions && (
        <table>
          <thead>
            <tr>
              <th>日付</th>
              <th>カテゴリ</th>
              <th>内容</th>
              <th>金額</th>
              <th>操作</th>
            </tr>
          </thead>
          <tbody>
            {filtered.length === 0 ? (
              <tr>
                <td colSpan={5}>収支なし</td>
              </tr>
            ) : (
              filtered.map((tx) => {
                const cat = categories?.find((c) => c.id === tx.categoryId)
                const amountDisplay =
                  cat?.type === 'Income'
                    ? `+¥${tx.amount.toLocaleString()}`
                    : `-¥${tx.amount.toLocaleString()}`
                return (
                  <tr key={tx.id}>
                    <td>{tx.date}</td>
                    <td>{tx.categoryName}</td>
                    <td>{tx.memo ?? '—'}</td>
                    <td>{amountDisplay}</td>
                    <td>
                      <button type="button" onClick={() => openEditModal(tx.id)}>
                        編集
                      </button>
                      <button
                        type="button"
                        onClick={() => handleDelete(tx.id)}
                        disabled={deleteMutation.isPending}
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
      )}

      {modalMode !== null && (
        <div
          role="dialog"
          aria-modal="true"
          aria-label={modalMode === 'create' ? '新規登録' : '収支編集'}
          style={{ position: 'fixed', inset: 0, background: 'rgba(0,0,0,0.4)' }}
        >
          <div style={{ background: '#fff', margin: '10vh auto', padding: '2rem', maxWidth: '400px' }}>
            <h3>{modalMode === 'create' ? '新規登録' : '収支編集'}</h3>
            <form onSubmit={handleSubmit}>
              <div>
                <label htmlFor="form-type">種別</label>
                <select
                  id="form-type"
                  value={formType}
                  onChange={(e) => {
                    setFormType(e.target.value as CategoryType)
                    setFormCategoryId('')
                  }}
                >
                  <option value="Expense">支出</option>
                  <option value="Income">収入</option>
                </select>
              </div>
              <div>
                <label htmlFor="form-category">カテゴリ</label>
                <select
                  id="form-category"
                  value={formCategoryId}
                  onChange={(e) => setFormCategoryId(e.target.value)}
                  required
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
                <label htmlFor="form-amount">金額</label>
                <input
                  id="form-amount"
                  type="number"
                  min="1"
                  value={formAmount}
                  onChange={(e) => setFormAmount(e.target.value)}
                  required
                />
              </div>
              <div>
                <label htmlFor="form-date">日付</label>
                <input
                  id="form-date"
                  type="date"
                  value={formDate}
                  onChange={(e) => setFormDate(e.target.value)}
                  required
                />
              </div>
              <div>
                <label htmlFor="form-memo">メモ</label>
                <input
                  id="form-memo"
                  type="text"
                  value={formMemo}
                  onChange={(e) => setFormMemo(e.target.value)}
                />
              </div>
              <button
                type="submit"
                disabled={createMutation.isPending || updateMutation.isPending}
              >
                {modalMode === 'create' ? '登録' : '更新'}
              </button>
              <button type="button" onClick={closeModal}>
                キャンセル
              </button>
            </form>
          </div>
        </div>
      )}

      {toastMessage && (
        <div role="alert" style={{ position: 'fixed', bottom: '1rem', right: '1rem' }}>
          {toastMessage}
        </div>
      )}
    </div>
  )
}

export default TransactionsPage
