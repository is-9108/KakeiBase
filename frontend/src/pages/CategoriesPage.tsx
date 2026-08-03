import { useState, useEffect, type FormEvent } from 'react'
import { useCategories, useCreateCategory, useUpdateCategory, useDeleteCategory } from '../hooks/useCategories'
import type { Category, CategoryType } from '../api/categories'
import Header from '../components/Header'

function CategoriesPage() {
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

  const typeBadgeClass = (type: CategoryType) =>
    type === 'Income'
      ? 'inline-flex px-2 py-0.5 rounded-full text-xs font-medium bg-green-100 text-green-700'
      : 'inline-flex px-2 py-0.5 rounded-full text-xs font-medium bg-red-100 text-red-700'

  return (
    <div className="min-h-screen">
      <Header />

      <main className="mx-auto max-w-7xl px-4 py-6">
        <h2 className="text-xl font-semibold text-gray-900 mb-6">カテゴリ管理</h2>

        {/* 追加フォーム */}
        <div className="bg-white rounded-xl shadow-sm border border-gray-100 p-5 mb-6">
          <form onSubmit={handleCreate} className="flex flex-wrap items-end gap-3">
            <div className="flex-1 min-w-40">
              <label className="block text-sm font-medium text-gray-700 mb-1" htmlFor="new-category-name">
                カテゴリ名
              </label>
              <input
                id="new-category-name"
                type="text"
                value={newName}
                onChange={(e) => setNewName(e.target.value)}
                placeholder="カテゴリ名"
                aria-label="カテゴリ名"
                required
                className="w-full px-3 py-2 border border-gray-300 rounded-lg focus:outline-none focus:ring-2 focus:ring-blue-500 text-sm"
              />
            </div>
            <div>
              <label className="block text-sm font-medium text-gray-700 mb-1" htmlFor="new-category-type">
                種別
              </label>
              <select
                id="new-category-type"
                value={newType}
                onChange={(e) => setNewType(e.target.value as CategoryType)}
                aria-label="種別"
                className="px-3 py-2 border border-gray-300 rounded-lg focus:outline-none focus:ring-2 focus:ring-blue-500 text-sm"
              >
                <option value="Expense">支出</option>
                <option value="Income">収入</option>
              </select>
            </div>
            <button
              type="submit"
              disabled={createMutation.isPending}
              className="px-4 py-2 bg-blue-600 hover:bg-blue-700 disabled:bg-blue-300 text-white text-sm font-medium rounded-lg transition-colors"
            >
              追加
            </button>
          </form>
        </div>

        {isLoading && (
          <div role="status" aria-label="読み込み中" className="text-gray-500 py-8 text-center">
            読み込み中...
          </div>
        )}

        {categories && (
          <div className="bg-white rounded-xl shadow-sm border border-gray-100 overflow-hidden">
            <table className="w-full text-sm">
              <thead className="bg-gray-50">
                <tr>
                  <th className="px-5 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">
                    カテゴリ名
                  </th>
                  <th className="px-5 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">
                    種別
                  </th>
                  <th className="px-5 py-3 text-center text-xs font-medium text-gray-500 uppercase tracking-wider">
                    操作
                  </th>
                </tr>
              </thead>
              <tbody className="divide-y divide-gray-100">
                {categories.length === 0 ? (
                  <tr>
                    <td colSpan={3} className="px-5 py-8 text-center text-gray-400">
                      カテゴリなし
                    </td>
                  </tr>
                ) : (
                  categories.map((cat) =>
                    editingId === cat.id ? (
                      <tr key={cat.id} className="bg-blue-50">
                        <td className="px-5 py-3">
                          <input
                            value={editName}
                            onChange={(e) => setEditName(e.target.value)}
                            aria-label="カテゴリ名(編集)"
                            className="w-full px-3 py-1.5 border border-blue-300 rounded-lg focus:outline-none focus:ring-2 focus:ring-blue-500 text-sm"
                          />
                        </td>
                        <td className="px-5 py-3">
                          <select
                            value={editType}
                            onChange={(e) => setEditType(e.target.value as CategoryType)}
                            aria-label="種別(編集)"
                            className="px-3 py-1.5 border border-blue-300 rounded-lg focus:outline-none focus:ring-2 focus:ring-blue-500 text-sm"
                          >
                            <option value="Expense">支出</option>
                            <option value="Income">収入</option>
                          </select>
                        </td>
                        <td className="px-5 py-3 text-center">
                          <button
                            type="button"
                            onClick={handleUpdate}
                            disabled={updateMutation.isPending}
                            className="px-2.5 py-1 text-xs bg-blue-600 hover:bg-blue-700 disabled:bg-blue-300 text-white rounded transition-colors mr-1"
                          >
                            保存
                          </button>
                          <button
                            type="button"
                            onClick={() => setEditingId(null)}
                            className="px-2.5 py-1 text-xs bg-gray-100 hover:bg-gray-200 text-gray-700 rounded transition-colors"
                          >
                            キャンセル
                          </button>
                        </td>
                      </tr>
                    ) : (
                      <tr key={cat.id} className="hover:bg-gray-50">
                        <td className="px-5 py-3 text-gray-900">{cat.name}</td>
                        <td className="px-5 py-3">
                          <span className={typeBadgeClass(cat.type)}>
                            {cat.type === 'Income' ? '収入' : '支出'}
                          </span>
                        </td>
                        <td className="px-5 py-3 text-center">
                          <button
                            type="button"
                            onClick={() => startEdit(cat)}
                            className="px-2.5 py-1 text-xs bg-gray-100 hover:bg-gray-200 text-gray-700 rounded transition-colors mr-1"
                          >
                            編集
                          </button>
                          <button
                            type="button"
                            onClick={() => handleDelete(cat.id)}
                            disabled={deleteMutation.isPending}
                            className="px-2.5 py-1 text-xs bg-red-50 hover:bg-red-100 text-red-600 rounded transition-colors disabled:opacity-50"
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
          </div>
        )}
      </main>

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

export default CategoriesPage
