import { Route, Routes } from 'react-router-dom'
import LoginPage from './pages/LoginPage'
import DashboardPage from './pages/DashboardPage'
import CategoriesPage from './pages/CategoriesPage'
import TransactionsPage from './pages/TransactionsPage'
import SubscriptionsPage from './pages/SubscriptionsPage'

function App() {
  return (
    <Routes>
      <Route path="/login" element={<LoginPage />} />
      <Route path="/" element={<DashboardPage />} />
      <Route path="/categories" element={<CategoriesPage />} />
      <Route path="/transactions" element={<TransactionsPage />} />
      <Route path="/subscriptions" element={<SubscriptionsPage />} />
    </Routes>
  )
}

export default App
