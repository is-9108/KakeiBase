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
    <div>
      <h1>KakeiBase</h1>
      <form onSubmit={handleSubmit} noValidate>
        <div>
          <label htmlFor="email">メールアドレス</label>
          <input
            id="email"
            type="email"
            value={email}
            onChange={(e) => setEmail(e.target.value)}
          />
          {fieldErrors.email && <p role="alert">{fieldErrors.email}</p>}
        </div>
        <div>
          <label htmlFor="password">パスワード</label>
          <input
            id="password"
            type="password"
            value={password}
            onChange={(e) => setPassword(e.target.value)}
          />
          {fieldErrors.password && <p role="alert">{fieldErrors.password}</p>}
        </div>
        {submitError && <p role="alert">{submitError}</p>}
        <button type="submit" disabled={isSubmitting}>
          ログイン
        </button>
      </form>
    </div>
  )
}

export default LoginPage
