import { useEffect, useMemo, useRef, useState } from 'react'
import type { Layout } from 'react-grid-layout'
import { api } from './api'
import type { Dashboard, FolderItem, ServiceItem, WidgetItem } from './types'
import { WallpaperBackground } from './components/WallpaperBackground'
import { DashboardGrid, type GridItem } from './components/DashboardGrid'
import { LoginModal } from './components/LoginModal'
import { ServiceModal } from './components/ServiceModal'
import { WidgetModal } from './components/WidgetModal'
import { FolderModal } from './components/FolderModal'
import { FolderView } from './components/FolderView'
import { FileBrowseButton } from './components/FileBrowseButton'

export default function App() {
  const [dashboard, setDashboard] = useState<Dashboard | null>(null)
  const [user, setUser] = useState<string | null>(null)
  const [editing, setEditing] = useState(false)
  const [loginOpen, setLoginOpen] = useState(false)
  const [serviceModal, setServiceModal] = useState<ServiceItem | null | 'new'>(null)
  const [widgetModal, setWidgetModal] = useState<WidgetItem | null | 'new'>(null)
  const [folderModal, setFolderModal] = useState<FolderItem | null | 'new'>(null)
  const [openFolderId, setOpenFolderId] = useState<string | null>(null)
  const [layoutDirty, setLayoutDirty] = useState(false)
  const [pendingLayout, setPendingLayout] = useState<Layout[] | null>(null)
  const [error, setError] = useState<string | null>(null)
  const [saving, setSaving] = useState(false)
  const wrapRef = useRef<HTMLDivElement>(null)
  const [width, setWidth] = useState(1200)

  const load = async () => {
    const data = await api.getDashboard()
    setDashboard(data)
    setLayoutDirty(false)
    setPendingLayout(null)
  }

  useEffect(() => {
    void load().catch((e) => setError(e instanceof Error ? e.message : 'Failed to load'))
    void api
      .me()
      .then((m) => setUser(m.username))
      .catch(() => setUser(null))
  }, [])

  useEffect(() => {
    const el = wrapRef.current
    if (!el) return
    const ro = new ResizeObserver((entries) => {
      const w = entries[0]?.contentRect.width
      if (w) setWidth(w)
    })
    ro.observe(el)
    setWidth(el.clientWidth)
    return () => ro.disconnect()
  }, [dashboard])

  useEffect(() => {
    if (!dashboard || editing) return
    const id = window.setInterval(() => {
      void api.getDashboard().then(setDashboard).catch(() => undefined)
    }, 30000)
    return () => window.clearInterval(id)
  }, [dashboard, editing])

  const openFolder = useMemo(
    () => dashboard?.folders.find((f) => f.id === openFolderId) ?? null,
    [dashboard, openFolderId],
  )

  const folderServices = useMemo(() => {
    if (!dashboard || !openFolderId) return []
    return dashboard.services.filter((s) => s.folderId === openFolderId)
  }, [dashboard, openFolderId])

  const items: GridItem[] = useMemo(() => {
    if (!dashboard) return []
    return [
      ...dashboard.folders.map((f) => ({ kind: 'folder' as const, data: f })),
      ...dashboard.services
        .filter((s) => !s.folderId)
        .map((s) => ({ kind: 'service' as const, data: s })),
      ...dashboard.widgets.map((w) => ({ kind: 'widget' as const, data: w })),
    ]
  }, [dashboard])

  const onLogin = async (username: string, password: string) => {
    const res = await api.login(username, password)
    setUser(res.username)
    setEditing(true)
  }

  const onLogout = async () => {
    await api.logout()
    setUser(null)
    setEditing(false)
  }

  const saveLayout = async () => {
    if (!pendingLayout) return
    setSaving(true)
    try {
      const payload = pendingLayout.map((l) => {
        const [kind, id] = l.i.split(':')
        return {
          id,
          kind,
          gridX: l.x,
          gridY: l.y,
          gridW: l.w,
          gridH: l.h,
        }
      })
      await api.saveLayout(payload)
      await load()
    } catch (e) {
      setError(e instanceof Error ? e.message : 'Failed to save layout')
    } finally {
      setSaving(false)
    }
  }

  const uploadWallpaper = async (file: File | null) => {
    if (!file) return
    setSaving(true)
    try {
      await api.uploadWallpaper(file)
      await load()
    } catch (e) {
      setError(e instanceof Error ? e.message : 'Wallpaper upload failed')
    } finally {
      setSaving(false)
    }
  }

  const clearWallpaper = async () => {
    setSaving(true)
    try {
      await api.clearWallpaper()
      await load()
    } catch (e) {
      setError(e instanceof Error ? e.message : 'Failed to clear wallpaper')
    } finally {
      setSaving(false)
    }
  }

  return (
    <div className="app-shell">
      {dashboard && <WallpaperBackground wallpaper={dashboard.wallpaper} />}

      <header className="topbar">
        <div className="brand">
          Home <span>Dashboard</span>
        </div>
        <div className="toolbar">
          {!user && (
            <button type="button" className="btn" onClick={() => setLoginOpen(true)}>
              Sign in
            </button>
          )}
          {user && (
            <>
              <button
                type="button"
                className={`btn${editing ? ' primary' : ''}`}
                onClick={() => setEditing((v) => !v)}
              >
                {editing ? 'Exit edit' : 'Edit'}
              </button>
              {editing && (
                <>
                  <button type="button" className="btn" onClick={() => setServiceModal('new')}>
                    + Service
                  </button>
                  <button type="button" className="btn" onClick={() => setFolderModal('new')}>
                    + Folder
                  </button>
                  <button type="button" className="btn" onClick={() => setWidgetModal('new')}>
                    + Widget
                  </button>
                  <FileBrowseButton
                    buttonText="Browse wallpaper"
                    accept="image/*,video/mp4,video/webm,.gif"
                    disabled={saving}
                    onFile={(file) => void uploadWallpaper(file)}
                  />
                  <button type="button" className="btn" onClick={() => void clearWallpaper()} disabled={saving}>
                    Clear wallpaper
                  </button>
                  <button
                    type="button"
                    className="btn primary"
                    disabled={!layoutDirty || saving}
                    onClick={() => void saveLayout()}
                  >
                    Save layout
                  </button>
                </>
              )}
              <button type="button" className="btn danger" onClick={() => void onLogout()}>
                Logout ({user})
              </button>
            </>
          )}
        </div>
      </header>

      {error && (
        <div className="empty-hint" style={{ marginTop: '1rem' }}>
          <div className="error">{error}</div>
          <button type="button" className="btn" style={{ marginTop: '0.8rem' }} onClick={() => setError(null)}>
            Dismiss
          </button>
        </div>
      )}

      <main className="dashboard-wrap" ref={wrapRef}>
        {dashboard && (
          <DashboardGrid
            items={items}
            editing={editing}
            width={width}
            onLayoutChange={(layout) => {
              if (!editing) return
              setPendingLayout(layout)
              setLayoutDirty(true)
            }}
            onEditService={(s) => setServiceModal(s)}
            onDeleteService={(s) => {
              if (!confirm(`Delete service "${s.title}"?`)) return
              void api
                .deleteService(s.id)
                .then(load)
                .catch((e) => setError(e instanceof Error ? e.message : 'Delete failed'))
            }}
            onEditWidget={(w) => setWidgetModal(w)}
            onDeleteWidget={(w) => {
              if (!confirm(`Delete ${w.type} widget?`)) return
              void api
                .deleteWidget(w.id)
                .then(load)
                .catch((e) => setError(e instanceof Error ? e.message : 'Delete failed'))
            }}
            onOpenFolder={(f) => setOpenFolderId(f.id)}
            onEditFolder={(f) => setFolderModal(f)}
            onDeleteFolder={(f) => {
              if (!confirm(`Delete folder "${f.title}"? Services inside will move to Home.`)) return
              void api
                .deleteFolder(f.id)
                .then(async () => {
                  if (openFolderId === f.id) setOpenFolderId(null)
                  await load()
                })
                .catch((e) => setError(e instanceof Error ? e.message : 'Delete failed'))
            }}
          />
        )}
      </main>

      {openFolder && (
        <FolderView
          folder={openFolder}
          services={folderServices}
          editing={editing}
          onClose={() => setOpenFolderId(null)}
          onEditService={(s) => setServiceModal(s)}
          onDeleteService={(s) => {
            if (!confirm(`Delete service "${s.title}"?`)) return
            void api
              .deleteService(s.id)
              .then(load)
              .catch((e) => setError(e instanceof Error ? e.message : 'Delete failed'))
          }}
          onAddService={() => setServiceModal('new')}
          onEditFolder={() => setFolderModal(openFolder)}
        />
      )}

      <LoginModal open={loginOpen} onClose={() => setLoginOpen(false)} onLogin={onLogin} />
      <ServiceModal
        open={serviceModal !== null}
        initial={serviceModal === 'new' ? null : serviceModal}
        folders={dashboard?.folders ?? []}
        defaultFolderId={serviceModal === 'new' ? openFolderId : null}
        onClose={() => setServiceModal(null)}
        onSaved={() => void load()}
      />
      <FolderModal
        open={folderModal !== null}
        initial={folderModal === 'new' ? null : folderModal}
        onClose={() => setFolderModal(null)}
        onSaved={() => void load()}
      />
      <WidgetModal
        open={widgetModal !== null}
        initial={widgetModal === 'new' ? null : widgetModal}
        onClose={() => setWidgetModal(null)}
        onSaved={() => void load()}
      />
    </div>
  )
}
