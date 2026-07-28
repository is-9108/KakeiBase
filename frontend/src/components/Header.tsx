import { NavLink, useNavigate } from 'react-router-dom'
import { logout } from '../api/auth'

function Header() {
  const navigate = useNavigate()

  async function handleLogout() {
    await logout().catch(() => {})
    navigate('/login', { replace: true })
  }

  return (
    <header>
      <h1>KakeiBase</h1>
      <nav>
        <NavLink to="/">ダッシュボード</NavLink>
        <NavLink to="/transactions">収支一覧</NavLink>
        <NavLink to="/categories">カテゴリ管理</NavLink>
        <NavLink to="/subscriptions">サブスク</NavLink>
        <button type="button" onClick={handleLogout}>
          ログアウト
        </button>
      </nav>
    </header>
  )
}

export default Header
