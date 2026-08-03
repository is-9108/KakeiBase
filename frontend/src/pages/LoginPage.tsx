import { useEffect, useRef, useState } from 'react'
import type { FormEvent } from 'react'
import { useNavigate } from 'react-router-dom'
import { login, refresh } from '../api/auth'

const EMAIL_PATTERN = /^[^\s@]+@[^\s@]+\.[^\s@]+$/

type FieldErrors = {
  email?: string
  password?: string
}

function validate(email: string, password: string): FieldErrors {
  const errors: FieldErrors = {}
  if (email.trim().length === 0) {
    errors.email = 'メールアドレスを入力してください'
  } else if (!EMAIL_PATTERN.test(email)) {
    errors.email = 'メールアドレスの形式が正しくありません'
  }
  if (password.length === 0) {
    errors.password = 'パスワードを入力してください'
  }
  return errors
}

function LoginPage() {
  const navigate = useNavigate()
  const [email, setEmail] = useState('')
  const [password, setPassword] = useState('')
  const [fieldErrors, setFieldErrors] = useState<FieldErrors>({})
  const [submitError, setSubmitError] = useState<string | null>(null)
  const [isSubmitting, setIsSubmitting] = useState(false)
  const [isCheckingSession, setIsCheckingSession] = useState(true)
  const isSubmittingRef = useRef(false)

  useEffect(() => {
    let cancelled = false
    refresh()
      .then(() => {
        if (!cancelled) navigate('/', { replace: true })
      })
      .catch(() => {
        if (!cancelled) setIsCheckingSession(false)
      })
    return () => {
      cancelled = true
    }
  }, [navigate])

  async function handleSubmit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault()
    if (isSubmittingRef.current) return

    const errors = validate(email, password)
    setFieldErrors(errors)
    if (Object.keys(errors).length > 0) return

    setSubmitError(null)
    isSubmittingRef.current = true
    setIsSubmitting(true)
    try {
      await login({ email, password })
      navigate('/', { replace: true })
    } catch {
      setSubmitError('ログインに失敗しました')
    } finally {
      isSubmittingRef.current = false
      setIsSubmitting(false)
    }
  }

  if (isCheckingSession) {
    return null
  }

  return (
    <div className="min-h-screen bg-gray-50 flex items-center justify-center px-4">
      <div className="w-full max-w-md bg-white rounded-2xl shadow-md p-8">
        <h1 className="text-2xl font-bold text-center text-gray-900 mb-8">KakeiBase</h1>
        <form onSubmit={handleSubmit} noValidate className="space-y-5">
          <div>
            <label htmlFor="email" className="block text-sm font-medium text-gray-700 mb-1">
              メールアドレス
            </label>
            <input
              id="email"
              type="email"
              value={email}
              onChange={(e) => setEmail(e.target.value)}
              className="w-full px-3 py-2 border border-gray-300 rounded-lg focus:outline-none focus:ring-2 focus:ring-blue-500 focus:border-transparent"
            />
            {fieldErrors.email && (
              <p role="alert" className="mt-1 text-sm text-red-600">
                {fieldErrors.email}
              </p>
            )}
          </div>
          <div>
            <label htmlFor="password" className="block text-sm font-medium text-gray-700 mb-1">
              パスワード
            </label>
            <input
              id="password"
              type="password"
              value={password}
              onChange={(e) => setPassword(e.target.value)}
              className="w-full px-3 py-2 border border-gray-300 rounded-lg focus:outline-none focus:ring-2 focus:ring-blue-500 focus:border-transparent"
            />
            {fieldErrors.password && (
              <p role="alert" className="mt-1 text-sm text-red-600">
                {fieldErrors.password}
              </p>
            )}
          </div>
          {submitError && (
            <p role="alert" className="text-sm text-red-600 text-center">
              {submitError}
            </p>
          )}
          <button
            type="submit"
            disabled={isSubmitting}
            className="w-full py-2.5 px-4 bg-blue-600 hover:bg-blue-700 disabled:bg-blue-300 text-white font-medium rounded-lg transition-colors"
          >
            ログイン
          </button>
        </form>
      </div>
    </div>
  )
}

export default LoginPage
