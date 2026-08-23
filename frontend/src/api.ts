export type PlatformOverrides = {
  iosUrl?: string | null
  androidUrl?: string | null
}

export type Link = {
  id: string
  shortCode: string
  shortUrl: string
  originalUrl: string
  platformOverrides: PlatformOverrides
  clickCount: number
  isActive: boolean
  createdAtUtc: string
  lastAccessedAtUtc: string | null
}

export type CreateLinkInput = {
  originalUrl: string
  customAlias?: string
  platformOverrides?: {
    ios?: string
    android?: string
  }
}

const apiBaseUrl = (import.meta.env.VITE_API_BASE_URL || 'http://localhost:5000').replace(/\/$/, '')

export class ApiError extends Error {
  readonly status: number

  constructor(
    message: string,
    status: number,
  ) {
    super(message)
    this.name = 'ApiError'
    this.status = status
  }
}

async function request<T>(path: string, init?: RequestInit): Promise<T> {
  let response: Response
  try {
    response = await fetch(`${apiBaseUrl}${path}`, {
      ...init,
      headers: {
        ...(init?.body ? { 'Content-Type': 'application/json' } : {}),
        ...init?.headers,
      },
    })
  } catch {
    throw new ApiError('The API could not be reached. Is the backend running?', 0)
  }

  if (!response.ok) {
    let message = `Request failed (${response.status}).`
    try {
      const problem = (await response.json()) as { detail?: string; title?: string }
      message = problem.detail || problem.title || message
    } catch {
      // Keep the status-based message when the response has no JSON body.
    }
    throw new ApiError(message, response.status)
  }

  if (response.status === 204) {
    return undefined as T
  }
  return (await response.json()) as T
}

export function listLinks(): Promise<Link[]> {
  return request<Link[]>('/api/links')
}

export function createLink(input: CreateLinkInput): Promise<Link> {
  return request<Link>('/api/links', {
    method: 'POST',
    body: JSON.stringify(input),
  })
}

export function getLinkStats(code: string): Promise<Link> {
  return request<Link>(`/api/links/${encodeURIComponent(code)}/stats`)
}

export function updateLinkStatus(code: string, isActive: boolean): Promise<void> {
  return request<void>(`/api/links/${encodeURIComponent(code)}/status`, {
    method: 'PATCH',
    body: JSON.stringify({ isActive }),
  })
}

export function deleteLink(code: string): Promise<void> {
  return request<void>(`/api/links/${encodeURIComponent(code)}`, { method: 'DELETE' })
}
