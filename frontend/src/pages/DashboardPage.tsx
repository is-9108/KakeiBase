import { useState, useEffect } from 'react'
import { PieChart, Pie, Cell, Tooltip, Legend } from 'recharts'
import { useDashboardSummary } from '../hooks/useDashboardSummary'
import Header from '../components/Header'

const PIE_COLORS = ['#0088FE', '#00C49F', '#FFBB28', '#FF8042', '#8884D8', '#82CA9D']

function DashboardPage() {
  const now = new Date()
  const [year, setYear] = useState(now.getFullYear())
  const [month, setMonth] = useState(now.getMonth() + 1)
  const [toastMessage, setToastMessage] = useState<string | null>(null)

  const { data, isLoading, isError } = useDashboardSummary(year, month)

  useEffect(() => {
    if (isError) setToastMessage('データの取得に失敗しました')
  }, [isError])

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

  return (
    <div className="min-h-screen">
      <Header />

      <main className="mx-auto max-w-7xl px-4 py-6">
        {/* 月ナビゲーション */}
        <div className="flex items-center gap-4 mb-6">
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

        {isLoading && (
          <div role="status" aria-label="読み込み中" className="text-gray-500 py-8 text-center">
            読み込み中...
          </div>
        )}

        {data && (
          <>
            {/* サマリーカード */}
            <div className="grid grid-cols-1 sm:grid-cols-3 gap-4 mb-8">
              <div className="bg-white rounded-xl shadow-sm border border-gray-100 p-5 text-center">
                <p className="text-sm text-gray-500 mb-1">収入</p>
                <p className="text-2xl font-bold text-green-600">
                  +¥{data.totalIncome.toLocaleString()}
                </p>
              </div>
              <div className="bg-white rounded-xl shadow-sm border border-gray-100 p-5 text-center">
                <p className="text-sm text-gray-500 mb-1">支出</p>
                <p className="text-2xl font-bold text-red-600">
                  -¥{data.totalExpense.toLocaleString()}
                </p>
              </div>
              <div className="bg-white rounded-xl shadow-sm border border-gray-100 p-5 text-center">
                <p className="text-sm text-gray-500 mb-1">残高</p>
                <p
                  className={`text-2xl font-bold ${data.balance >= 0 ? 'text-gray-900' : 'text-red-600'}`}
                >
                  ¥{data.balance.toLocaleString()}
                </p>
              </div>
            </div>

            {/* カテゴリ別円グラフ */}
            {data.categoryBreakdown.length > 0 && (
              <div className="bg-white rounded-xl shadow-sm border border-gray-100 p-5 mb-8 flex justify-center">
                <PieChart width={400} height={300}>
                  <Pie
                    data={data.categoryBreakdown}
                    dataKey="amount"
                    nameKey="categoryName"
                    cx="50%"
                    cy="50%"
                    outerRadius={100}
                  >
                    {data.categoryBreakdown.map((_, index) => (
                      <Cell key={`cell-${index}`} fill={PIE_COLORS[index % PIE_COLORS.length]} />
                    ))}
                  </Pie>
                  <Tooltip />
                  <Legend />
                </PieChart>
              </div>
            )}

            {/* 直近の取引 */}
            <div className="bg-white rounded-xl shadow-sm border border-gray-100 overflow-hidden">
              <div className="px-5 py-4 border-b border-gray-100">
                <h2 className="font-semibold text-gray-900">直近の取引</h2>
              </div>
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
                      種別
                    </th>
                    <th className="px-5 py-3 text-right text-xs font-medium text-gray-500 uppercase tracking-wider">
                      金額
                    </th>
                    <th className="px-5 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">
                      メモ
                    </th>
                  </tr>
                </thead>
                <tbody className="divide-y divide-gray-100">
                  {data.recentTransactions.length === 0 ? (
                    <tr>
                      <td colSpan={5} className="px-5 py-8 text-center text-gray-400">
                        取引なし
                      </td>
                    </tr>
                  ) : (
                    data.recentTransactions.map((tx) => (
                      <tr key={tx.id} className="hover:bg-gray-50">
                        <td className="px-5 py-3 text-gray-600">{tx.date}</td>
                        <td className="px-5 py-3 text-gray-900">{tx.categoryName}</td>
                        <td className="px-5 py-3">
                          <span
                            className={`inline-flex px-2 py-0.5 rounded-full text-xs font-medium ${
                              tx.type === 'Income'
                                ? 'bg-green-100 text-green-700'
                                : 'bg-red-100 text-red-700'
                            }`}
                          >
                            {tx.type === 'Income' ? '収入' : '支出'}
                          </span>
                        </td>
                        <td
                          className={`px-5 py-3 text-right font-medium ${
                            tx.type === 'Income' ? 'text-green-600' : 'text-red-600'
                          }`}
                        >
                          {tx.type === 'Income' ? '+' : '-'}¥{tx.amount.toLocaleString()}
                        </td>
                        <td className="px-5 py-3 text-gray-500">{tx.memo ?? '—'}</td>
                      </tr>
                    ))
                  )}
                </tbody>
              </table>
            </div>
          </>
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

export default DashboardPage
