import { describe, it, expect } from 'vitest'
import { render, screen } from '@testing-library/react'
import { QueryClient, QueryClientProvider } from '@tanstack/react-query'
import { MemoryRouter } from 'react-router-dom'
import type { ReactElement } from 'react'
import App from '../App'

function renderWithProviders(ui: ReactElement, initialEntries: string[] = ['/']) {
  const queryClient = new QueryClient({
    defaultOptions: { queries: { retry: false } },
  })
  return render(
    <QueryClientProvider client={queryClient}>
      <MemoryRouter initialEntries={initialEntries}>{ui}</MemoryRouter>
    </QueryClientProvider>,
  )
}

describe('App', () => {
  it('/ でKakeiBaseの見出しを表示する', () => {
    renderWithProviders(<App />)
    expect(screen.getByRole('heading', { name: 'KakeiBase' })).toBeInTheDocument()
  })
})
