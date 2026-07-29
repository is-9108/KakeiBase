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

  return (
    <div>
      <Header />

      <h2>サブスク管理</h2>

      <div>
        <button type="button" onClick={openCreateModal}>
          + 追加
        </button>
      </div>

      {isLoading && (
        <div role="status" aria-label="読み込み中">
          読み込み中...
        </div>
      )}

      {subscriptions && (
        <>
          <h3>有効なサブスク</h3>
          <table>
            <thead>
              <tr>
                <th>サービス名</th>
                <th>金額</th>
                <th>操作</th>
              </tr>
            </thead>
            <tbody>
              {active.length === 0 ? (
                <tr>
                  <td colSpan={3}>有効なサブスクはありません</td>
                </tr>
              ) : (
                active.map((sub) => (
                  <tr key={sub.id}>
                    <td>{sub.name}</td>
                    <td>¥{sub.amount.toLocaleString()}</td>
                    <td>
                      <button
                        type="button"
                        onClick={() => handleToggleActive(sub)}
                        disabled={updateMutation.isPending}
                      >
                        停止
                      </button>
                      <button type="button" onClick={() => openEditModal(sub)}>
                        編集
                      </button>
                    </td>
                  </tr>
                ))
              )}
            </tbody>
          </table>

          <h3>停止中のサブスク</h3>
          <table>
            <thead>
              <tr>
                <th>サービス名</th>
                <th>金額</th>
                <th>操作</th>
              </tr>
            </thead>
            <tbody>
              {inactive.length === 0 ? (
                <tr>
                  <td colSpan={3}>停止中のサブスクはありません</td>
                </tr>
              ) : (
                inactive.map((sub) => (
                  <tr key={sub.id}>
                    <td>{sub.name}</td>
                    <td>¥{sub.amount.toLocaleString()}</td>
                    <td>
                      <button
                        type="button"
                        onClick={() => handleToggleActive(sub)}
                        disabled={updateMutation.isPending}
                      >
                        再開
                      </button>
                      <button
                        type="button"
                        onClick={() => handleDelete(sub.id)}
                        disabled={deleteMutation.isPending}
                      >
                        削除
                      </button>
                    </td>
                  </tr>
                ))
              )}
            </tbody>
          </table>
        </>
      )}

      {modalMode !== null && (
        <div
          role="dialog"
          aria-modal="true"
          aria-label={modalMode === 'create' ? 'サブスク追加' : 'サブスク編集'}
          style={{ position: 'fixed', inset: 0, background: 'rgba(0,0,0,0.4)' }}
        >
          <div style={{ background: '#fff', margin: '10vh auto', padding: '2rem', maxWidth: '400px' }}>
            <h3>{modalMode === 'create' ? 'サブスク追加' : 'サブスク編集'}</h3>
            <form onSubmit={handleSubmit}>
              <div>
                <label htmlFor="form-name">サービス名</label>
                <input
                  id="form-name"
                  type="text"
                  maxLength={100}
                  value={formName}
                  onChange={(e) => setFormName(e.target.value)}
                  required
                />
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

export default SubscriptionsPage
