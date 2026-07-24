import { useState, useEffect } from 'react'
import { useNavigate } from 'react-router-dom'
import { PieChart, Pie, Cell, Tooltip, Legend } from 'recharts'
import { logout } from '../api/auth'
import { useDashboardSummary } from '../hooks/useDashboardSummary'

const PIE_COLORS = ['#0088FE', '#00C49F', '#FFBB28', '#FF8042', '#8884D8', '#82CA9D']

function DashboardPage() {
  const navigate = useNavigate()
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

  async function handleLogout() {
    await logout().catch(() => {})
    navigate('/login', { replace: true })
  }

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
    <div>
      <header>
        <h1>KakeiBase</h1>
        <nav>
          <button type="button" onClick={handleLogout}>
            ログアウト
          </button>
        </nav>
      </header>

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
      </div>

      {isLoading && (
        <div role="status" aria-label="読み込み中">
          読み込み中...
        </div>
      )}

      {data && (
        <>
          <div>
            <div>
              <span>収入</span>
              <span>{data.totalIncome.toLocaleString()}円</span>
            </div>
            <div>
              <span>支出</span>
              <span>{data.totalExpense.toLocaleString()}円</span>
            </div>
            <div>
              <span>残高</span>
              <span>{data.balance.toLocaleString()}円</span>
            </div>
          </div>

          {data.categoryBreakdown.length > 0 && (
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
          )}

          <table>
            <thead>
              <tr>
                <th>日付</th>
                <th>カテゴリ</th>
                <th>種別</th>
                <th>金額</th>
                <th>メモ</th>
              </tr>
            </thead>
            <tbody>
              {data.recentTransactions.length === 0 ? (
                <tr>
                  <td colSpan={5}>取引なし</td>
                </tr>
              ) : (
                data.recentTransactions.map((tx) => (
                  <tr key={tx.id}>
                    <td>{tx.date}</td>
                    <td>{tx.categoryName}</td>
                    <td>{tx.type === 'Income' ? '収入' : '支出'}</td>
                    <td>{tx.amount.toLocaleString()}円</td>
                    <td>{tx.memo ?? '—'}</td>
                  </tr>
                ))
              )}
            </tbody>
          </table>
        </>
      )}

      {toastMessage && (
        <div role="alert" style={{ position: 'fixed', bottom: '1rem', right: '1rem' }}>
          {toastMessage}
        </div>
      )}
    </div>
  )
}

export default DashboardPage
