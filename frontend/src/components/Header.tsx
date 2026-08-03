import { NavLink, useNavigate } from 'react-router-dom'
import { logout } from '../api/auth'

function Header() {
  const navigate = useNavigate()

  async function handleLogout() {
    await logout().catch(() => {})
    navigate('/login', { replace: true })
  }

  const navLinkClass = ({ isActive }: { isActive: boolean }) =>
    isActive
      ? 'text-white font-medium'
      : 'text-gray-400 hover:text-white transition-colors'

  return (
    <header className="bg-gray-900 text-white">
      <div className="mx-auto max-w-7xl px-4 py-3 flex items-center justify-between">
        <span className="text-xl font-bold tracking-tight">KakeiBase</span>
        <nav className="flex items-center gap-6">
          <NavLink to="/" end className={navLinkClass}>
            ダッシュボード
          </NavLink>
          <NavLink to="/transactions" className={navLinkClass}>
            収支一覧
          </NavLink>
          <NavLink to="/categories" className={navLinkClass}>
            カテゴリ管理
          </NavLink>
          <NavLink to="/subscriptions" className={navLinkClass}>
            サブスク
          </NavLink>
          <button
            type="button"
            onClick={handleLogout}
            className="ml-2 px-3 py-1.5 text-sm bg-gray-700 hover:bg-gray-600 rounded-md transition-colors cursor-pointer"
          >
            ログアウト
          </button>
        </nav>
      </div>
    </header>
  )
}

export default Header
