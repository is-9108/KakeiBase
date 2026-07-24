import { Route, Routes } from 'react-router-dom'
import LoginPage from './pages/LoginPage'

function DashboardPage() {
  return (
    <div>
      <h1>KakeiBase</h1>
    </div>
  )
}

function App() {
  return (
    <Routes>
      <Route path="/login" element={<LoginPage />} />
      <Route path="/" element={<DashboardPage />} />
    </Routes>
  )
}

export default App
