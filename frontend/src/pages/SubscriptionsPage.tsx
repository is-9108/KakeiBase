import { useState, useEffect, type FormEvent } from 'react'
import {
  useSubscriptions,
  useCreateSubscription,
  useUpdateSubscription,
  useDeleteSubscription,
} from '../hooks/useSubscriptions'
import type { Subscription } from '../api/subscriptions'
import Header from '../components/Header'

function SubscriptionsPage() {
  // モーダル
  const [modalMode, setModalMode] = useState<null | 'create' | 'edit'>(null)
  const [editingId, setEditingId] = useState<string | null>(null)

  // フォーム(モーダル内共有)
  const [formName, setFormName] = useState('')
  const [formAmount, setFormAmount] = useState('')

  const [toastMessage, setToastMessage] = useState<string | null>(null)

  const { data: subscriptions, isLoading, isError } = useSubscriptions()
  const createMutation = useCreateSubscription()
  const updateMutation = useUpdateSubscription()
  const deleteMutation = useDeleteSubscription()

  useEffect(() => {
    if (isError) setToastMessage('データの取得に失敗しました')
  }, [isError])

  useEffect(() => {
    if (createMutation.isError) setToastMessage('サブスクの追加に失敗しました')
  }, [createMutation.isError])

  useEffect(() => {
    if (updateMutation.isError) setToastMessage('サブスクの更新に失敗しました')
  }, [updateMutation.isError])

  useEffect(() => {
    if (deleteMutation.isError) setToastMessage('サブスクの削除に失敗しました')
  }, [deleteMutation.isError])

  useEffect(() => {
    if (!toastMessage) return
    const id = setTimeout(() => setToastMessage(null), 3000)
    return () => clearTimeout(id)
  }, [toastMessage])

  function openCreateModal() {
    setModalMode('create')
    setEditingId(null)
    setFormName('')
    setFormAmount('')
  }

  function openEditModal(sub: Subscription) {
    setModalMode('edit')
    setEditingId(sub.id)
    setFormName(sub.name)
    setFormAmount(String(sub.amount))
  }

  function closeModal() {
    setModalMode(null)
    setEditingId(null)
  }

  function handleSubmit(e: FormEvent) {
    e.preventDefault()
    if (!formName || !formAmount) return

    if (modalMode === 'create') {
      createMutation.mutate(
        { name: formName, amount: Number(formAmount) },
        {
          onSuccess: () => {
            closeModal()
            setToastMessage('サブスクを追加しました')
          },
        },
      )
    } else if (modalMode === 'edit' && editingId) {
      const target = subscriptions?.find((s) => s.id === editingId)
      if (!target) return
      updateMutation.mutate(
        {
          id: editingId,
          body: {
            name: formName,
            amount: Number(formAmount),
            isActive: target.isActive,
          },
        },
        {
          onSuccess: () => {
            closeModal()
            setToastMessage('サブスクを更新しました')
          },
        },
      )
    }
  }

  function handleToggleActive(sub: Subscription) {
    updateMutation.mutate(
      {
        id: sub.id,
        body: { name: sub.name, amount: sub.amount, isActive: !sub.isActive },
      },
      {
        onSuccess: () => setToastMessage(sub.isActive ? 'サブスクを停止しました' : 'サブスクを再開しました'),
      },
    )
  }

  function handleDelete(id: string) {
    if (!window.confirm('このサブスクを削除しますか？')) return
    deleteMutation.mutate(id, {
      onSuccess: () => setToastMessage('サブスクを削除しました'),
    })
  }

  const active = (subscriptions ?? []).filter((s) => s.isActive)
  const inactive = (subscriptions ?? []).filter((s) => !s.isActive)

  const inputClass = 'w-full px-3 py-2 border border-gray-300 rounded-lg focus:outline-none focus:ring-2 focus:ring-blue-500 focus:border-transparent text-sm'
  const labelClass = 'block text-sm font-medium text-gray-700 mb-1'

  function SubscriptionTable({
    items,
    isActive,
  }: {
    items: Subscription[]
    /** 有効なサブスクのテーブルかどうか */
    isActive: boolean
  }) {
    const emptyText = isActive ? '有効なサブスクはありません' : '停止中のサブスクはありません'
    return (
      <div className="bg-white rounded-xl shadow-sm border border-gray-100 overflow-hidden mb-6">
        <div className="px-5 py-4 border-b border-gray-100 flex items-center gap-2">
          <h3 className="font-semibold text-gray-900">{isActive ? '有効なサブスク' : '停止中のサブスク'}</h3>
          <span
            className={`inline-flex px-2 py-0.5 rounded-full text-xs font-medium ${
              isActive ? 'bg-green-100 text-green-700' : 'bg-gray-100 text-gray-600'
            }`}
          >
            {items.length}件
          </span>
        </div>
        <table className="w-full text-sm">
          <thead className="bg-gray-50">
            <tr>
              <th className="px-5 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">
                サービス名
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
            {items.length === 0 ? (
              <tr>
                <td colSpan={3} className="px-5 py-8 text-center text-gray-400">
                  {emptyText}
                </td>
              </tr>
            ) : (
              items.map((sub) => (
                <tr key={sub.id} className="hover:bg-gray-50">
                  <td className="px-5 py-3 text-gray-900">{sub.name}</td>
                  <td className="px-5 py-3 text-right font-medium text-gray-700">
                    ¥{sub.amount.toLocaleString()}
                  </td>
                  <td className="px-5 py-3 text-center">
                    {isActive ? (
                      <>
                        <button
                          type="button"
                          onClick={() => handleToggleActive(sub)}
                          disabled={updateMutation.isPending}
                          className="px-2.5 py-1 text-xs bg-yellow-50 hover:bg-yellow-100 text-yellow-700 rounded transition-colors disabled:opacity-50 mr-1"
                        >
                          停止
                        </button>
                        <button
                          type="button"
                          onClick={() => openEditModal(sub)}
                          className="px-2.5 py-1 text-xs bg-gray-100 hover:bg-gray-200 text-gray-700 rounded transition-colors"
                        >
                          編集
                        </button>
                      </>
                    ) : (
                      <>
                        <button
                          type="button"
                          onClick={() => handleToggleActive(sub)}
                          disabled={updateMutation.isPending}
                          className="px-2.5 py-1 text-xs bg-green-50 hover:bg-green-100 text-green-700 rounded transition-colors disabled:opacity-50 mr-1"
                        >
                          再開
                        </button>
                        <button
                          type="button"
                          onClick={() => handleDelete(sub.id)}
                          disabled={deleteMutation.isPending}
                          className="px-2.5 py-1 text-xs bg-red-50 hover:bg-red-100 text-red-600 rounded transition-colors disabled:opacity-50"
                        >
                          削除
                        </button>
                      </>
                    )}
                  </td>
                </tr>
              ))
            )}
          </tbody>
        </table>
      </div>
    )
  }

  return (
    <div className="min-h-screen">
      <Header />

      <main className="mx-auto max-w-7xl px-4 py-6">
        <div className="flex items-center justify-between mb-6">
          <h2 className="text-xl font-semibold text-gray-900">サブスク管理</h2>
          <button
            type="button"
            onClick={openCreateModal}
            className="px-4 py-2 bg-blue-600 hover:bg-blue-700 text-white text-sm font-medium rounded-lg transition-colors"
          >
            + 追加
          </button>
        </div>

        {isLoading && (
          <div role="status" aria-label="読み込み中" className="text-gray-500 py-8 text-center">
            読み込み中...
          </div>
        )}

        {subscriptions && (
          <>
            <SubscriptionTable items={active} isActive={true} />
            <SubscriptionTable items={inactive} isActive={false} />
          </>
        )}
      </main>

      {/* モーダル */}
      {modalMode !== null && (
        <div
          role="dialog"
          aria-modal="true"
          aria-label={modalMode === 'create' ? 'サブスク追加' : 'サブスク編集'}
          className="fixed inset-0 bg-black/40 flex items-center justify-center px-4 z-50"
        >
          <div className="bg-white rounded-2xl shadow-xl w-full max-w-md p-6">
            <h3 className="text-lg font-semibold text-gray-900 mb-5">
              {modalMode === 'create' ? 'サブスク追加' : 'サブスク編集'}
            </h3>
            <form onSubmit={handleSubmit} className="space-y-4">
              <div>
                <label htmlFor="form-name" className={labelClass}>
                  サービス名
                </label>
                <input
                  id="form-name"
                  type="text"
                  maxLength={100}
                  value={formName}
                  onChange={(e) => setFormName(e.target.value)}
                  required
                  className={inputClass}
                />
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

export default SubscriptionsPage
