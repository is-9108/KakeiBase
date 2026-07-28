import { useState, useEffect, type FormEvent } from 'react'
import { useNavigate } from 'react-router-dom'
import { useCategories, useCreateCategory, useUpdateCategory, useDeleteCategory } from '../hooks/useCategories'
import { logout } from '../api/auth'
import type { Category, CategoryType } from '../api/categories'

function CategoriesPage() {
  const navigate = useNavigate()

  const [newName, setNewName] = useState('')
  const [newType, setNewType] = useState<CategoryType>('Expense')
  const [editingId, setEditingId] = useState<string | null>(null)
  const [editName, setEditName] = useState('')
  const [editType, setEditType] = useState<CategoryType>('Expense')
  const [toastMessage, setToastMessage] = useState<string | null>(null)

  const { data: categories, isLoading, isError } = useCategories()
  const createMutation = useCreateCategory()
  const updateMutation = useUpdateCategory()
  const deleteMutation = useDeleteCategory()

  useEffect(() => {
    if (isError) setToastMessage('データの取得に失敗しました')
  }, [isError])

  useEffect(() => {
    if (createMutation.isError) setToastMessage('カテゴリの追加に失敗しました')
  }, [createMutation.isError])

  useEffect(() => {
    if (updateMutation.isError) setToastMessage('カテゴリの更新に失敗しました')
  }, [updateMutation.isError])

  useEffect(() => {
    if (deleteMutation.isError) setToastMessage('カテゴリの削除に失敗しました')
  }, [deleteMutation.isError])

  useEffect(() => {
    if (!toastMessage) return
    const id = setTimeout(() => setToastMessage(null), 3000)
    return () => clearTimeout(id)
  }, [toastMessage])

  async function handleLogout() {
    await logout().catch(() => {})
    navigate('/login', { replace: true })
  }

  function handleCreate(e: FormEvent) {
    e.preventDefault()
    if (!newName.trim()) return
    createMutation.mutate(
      { name: newName.trim(), type: newType },
      {
        onSuccess: () => {
          setNewName('')
          setNewType('Expense')
          setToastMessage('カテゴリを追加しました')
        },
      },
    )
  }

  function startEdit(category: Category) {
    setEditingId(category.id)
    setEditName(category.name)
    setEditType(category.type)
  }

  function handleUpdate(e: FormEvent) {
    e.preventDefault()
    if (!editingId || !editName.trim()) return
    updateMutation.mutate(
      { id: editingId, body: { name: editName.trim(), type: editType } },
      {
        onSuccess: () => {
          setEditingId(null)
          setToastMessage('カテゴリを更新しました')
        },
      },
    )
  }

  function handleDelete(id: string) {
    deleteMutation.mutate(id, {
      onSuccess: () => setToastMessage('カテゴリを削除しました'),
    })
  }

  return (
    <div>
      <header>
        <h1>KakeiBase</h1>
        <nav>
          <button type="button" onClick={() => navigate('/')}>
            ダッシュボード
          </button>
          <button type="button" onClick={handleLogout}>
            ログアウト
          </button>
        </nav>
      </header>

      <h2>カテゴリ管理</h2>

      <form onSubmit={handleCreate}>
        <input
          type="text"
          value={newName}
          onChange={(e) => setNewName(e.target.value)}
          placeholder="カテゴリ名"
          aria-label="カテゴリ名"
          required
        />
        <select
          value={newType}
          onChange={(e) => setNewType(e.target.value as CategoryType)}
          aria-label="種別"
        >
          <option value="Expense">支出</option>
          <option value="Income">収入</option>
        </select>
        <button type="submit" disabled={createMutation.isPending}>
          追加
        </button>
      </form>

      {isLoading && (
        <div role="status" aria-label="読み込み中">
          読み込み中...
        </div>
      )}

      {categories && (
        <table>
          <thead>
            <tr>
              <th>カテゴリ名</th>
              <th>種別</th>
              <th>操作</th>
            </tr>
          </thead>
          <tbody>
            {categories.length === 0 ? (
              <tr>
                <td colSpan={3}>カテゴリなし</td>
              </tr>
            ) : (
              categories.map((cat) =>
                editingId === cat.id ? (
                  <tr key={cat.id}>
                    <td>
                      <input
                        value={editName}
                        onChange={(e) => setEditName(e.target.value)}
                        aria-label="カテゴリ名(編集)"
                      />
                    </td>
                    <td>
                      <select
                        value={editType}
                        onChange={(e) => setEditType(e.target.value as CategoryType)}
                        aria-label="種別(編集)"
                      >
                        <option value="Expense">支出</option>
                        <option value="Income">収入</option>
                      </select>
                    </td>
                    <td>
                      <button
                        type="button"
                        onClick={handleUpdate}
                        disabled={updateMutation.isPending}
                      >
                        保存
                      </button>
                      <button type="button" onClick={() => setEditingId(null)}>
                        キャンセル
                      </button>
                    </td>
                  </tr>
                ) : (
                  <tr key={cat.id}>
                    <td>{cat.name}</td>
                    <td>{cat.type === 'Income' ? '収入' : '支出'}</td>
                    <td>
                      <button type="button" onClick={() => startEdit(cat)}>
                        編集
                      </button>
                      <button
                        type="button"
                        onClick={() => handleDelete(cat.id)}
                        disabled={deleteMutation.isPending}
                      >
                        削除
                      </button>
                    </td>
                  </tr>
                ),
              )
            )}
          </tbody>
        </table>
      )}

      {toastMessage && (
        <div role="alert" style={{ position: 'fixed', bottom: '1rem', right: '1rem' }}>
          {toastMessage}
        </div>
      )}
    </div>
  )
}

export default CategoriesPage
