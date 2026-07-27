import type { Dashboard, ServiceItem, WidgetItem, FolderItem, Wallpaper } from './types'

async function request<T>(url: string, init?: RequestInit): Promise<T> {
  const res = await fetch(url, {
    credentials: 'include',
    ...init,
    headers: {
      ...(init?.body instanceof FormData ? {} : { 'Content-Type': 'application/json' }),
      ...init?.headers,
    },
  })

  if (!res.ok) {
    let message = res.statusText
    try {
      const text = await res.text()
      if (text) {
        const data = JSON.parse(text) as { message?: string }
        message = data.message ?? message
      }
    } catch {
      /* ignore */
    }
    throw new Error(message || `HTTP ${res.status}`)
  }

  if (res.status === 204) return undefined as T

  const text = await res.text()
  if (!text) return undefined as T

  return JSON.parse(text) as T
}

export const api = {
  getDashboard: () => request<Dashboard>('/api/dashboard'),

  me: () => request<{ username: string }>('/api/auth/me'),
  login: (username: string, password: string) =>
    request<{ username: string }>('/api/auth/login', {
      method: 'POST',
      body: JSON.stringify({ username, password }),
    }),
  logout: () => request<void>('/api/auth/logout', { method: 'POST' }),

  createService: (body: Partial<ServiceItem> & { title: string; url: string }) =>
    request<ServiceItem>('/api/services', { method: 'POST', body: JSON.stringify(body) }),
  updateService: (id: string, body: object) =>
    request<ServiceItem>(`/api/services/${id}`, { method: 'PUT', body: JSON.stringify(body) }),
  deleteService: (id: string) =>
    request<void>(`/api/services/${id}`, { method: 'DELETE' }),

  createFolder: (body: object) =>
    request<FolderItem>('/api/folders', { method: 'POST', body: JSON.stringify(body) }),
  updateFolder: (id: string, body: object) =>
    request<FolderItem>(`/api/folders/${id}`, { method: 'PUT', body: JSON.stringify(body) }),
  deleteFolder: (id: string) =>
    request<void>(`/api/folders/${id}`, { method: 'DELETE' }),

  createWidget: (body: object) =>
    request<WidgetItem>('/api/widgets', { method: 'POST', body: JSON.stringify(body) }),
  updateWidget: (id: string, body: object) =>
    request<WidgetItem>(`/api/widgets/${id}`, { method: 'PUT', body: JSON.stringify(body) }),
  deleteWidget: (id: string) =>
    request<void>(`/api/widgets/${id}`, { method: 'DELETE' }),

  saveLayout: (items: { id: string; kind: string; gridX: number; gridY: number; gridW: number; gridH: number }[]) =>
    request<void>('/api/layout', { method: 'PUT', body: JSON.stringify({ items }) }),

  setWallpaper: (path: string | null, type: string) =>
    request<Wallpaper>('/api/settings/wallpaper', {
      method: 'PUT',
      body: JSON.stringify({ path, type }),
    }),

  uploadWallpaper: async (file: File) => {
    const form = new FormData()
    form.append('file', file)
    return request<Wallpaper>('/api/settings/wallpaper', {
      method: 'POST',
      body: form,
      headers: {},
    })
  },

  clearWallpaper: () => request<Wallpaper>('/api/settings/wallpaper', { method: 'DELETE' }),

  upload: async (file: File) => {
    const form = new FormData()
    form.append('file', file)
    return request<{ path: string; contentType: string }>('/api/uploads', {
      method: 'POST',
      body: form,
      headers: {},
    })
  },

  deleteUpload: (path: string) =>
    request<void>(`/api/uploads?path=${encodeURIComponent(path)}`, { method: 'DELETE' }),
}
