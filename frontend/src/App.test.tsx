import { cleanup, render, screen, waitFor } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest'
import App from './App'

const createdLink = {
  id: '1',
  shortCode: 'demo1234',
  shortUrl: 'http://localhost:5000/demo1234',
  originalUrl: 'https://example.com/',
  platformOverrides: { iosUrl: null, androidUrl: null },
  clickCount: 0,
  isActive: true,
  createdAtUtc: '2026-08-23T12:00:00Z',
  lastAccessedAtUtc: null,
}

describe('dashboard', () => {
  afterEach(() => cleanup())

  beforeEach(() => {
    vi.restoreAllMocks()
    vi.stubGlobal('fetch', vi.fn().mockResolvedValue(new Response(JSON.stringify([]), { status: 200 })))
  })

  it('reveals platform fields only when enabled and omits empty overrides', async () => {
    const user = userEvent.setup()
    const fetchMock = vi.mocked(fetch)
    render(<App />)

    await waitFor(() => expect(screen.queryByText('Loading your links...')).not.toBeInTheDocument())
    expect(screen.queryByLabelText('iOS destination')).not.toBeInTheDocument()

    await user.click(screen.getByLabelText('Platform destinations'))
    expect(screen.getByPlaceholderText('https://download.your-site.com/app.ipa')).toBeInTheDocument()
    await user.type(screen.getByPlaceholderText('https://your-site.com/landing'), 'https://example.com')
    await user.click(screen.getByRole('button', { name: /create short link/i }))

    await waitFor(() => expect(fetchMock).toHaveBeenCalledTimes(2))
    const postCall = fetchMock.mock.calls[1]
    expect(JSON.parse(String(postCall[1]?.body))).toEqual({ originalUrl: 'https://example.com' })
  })

  it('adds a created link to the dashboard', async () => {
    const user = userEvent.setup()
    vi.stubGlobal('fetch', vi.fn()
      .mockResolvedValueOnce(new Response(JSON.stringify([]), { status: 200 }))
      .mockResolvedValueOnce(new Response(JSON.stringify(createdLink), { status: 201 })))
    render(<App />)

    await user.type(screen.getByPlaceholderText('https://your-site.com/landing'), 'https://example.com')
    await user.click(screen.getByRole('button', { name: /create short link/i }))

    expect(await screen.findByText('localhost:5000/demo1234')).toBeInTheDocument()
  })
})
