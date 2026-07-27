import { useEffect, useState } from 'react'
import type { FormEvent } from 'react'
import { api } from '../api'
import type { FolderItem } from '../types'
import { FileBrowseButton } from './FileBrowseButton'

type Props = {
  open: boolean
  initial?: FolderItem | null
  onClose: () => void
  onSaved: () => void
}

export function FolderModal({ open, initial, onClose, onSaved }: Props) {
  const [title, setTitle] = useState('')
  const [imagePath, setImagePath] = useState<string | null>(null)
  const [error, setError] = useState<string | null>(null)
  const [busy, setBusy] = useState(false)

  useEffect(() => {
    if (!open) return
    setTitle(initial?.title ?? '')
    setImagePath(initial?.imagePath ?? null)
    setError(null)
  }, [open, initial])

  if (!open) return null

  const uploadImage = async (file: File | null) => {
    if (!file) return
    setBusy(true)
    try {
      const previous = imagePath
      const res = await api.upload(file)
      setImagePath(res.path)
      if (previous && previous !== res.path) {
        await api.deleteUpload(previous).catch(() => undefined)
      }
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Upload failed')
    } finally {
      setBusy(false)
    }
  }

  const removeImage = async () => {
    if (!imagePath) return
    setBusy(true)
    try {
      const path = imagePath
      setImagePath(null)
      await api.deleteUpload(path).catch(() => undefined)
    } finally {
      setBusy(false)
    }
  }

  const submit = async (e: FormEvent) => {
    e.preventDefault()
    setBusy(true)
    setError(null)
    try {
      if (initial) {
        await api.updateFolder(initial.id, {
          title,
          imagePath,
          gridX: initial.gridX,
          gridY: initial.gridY,
          gridW: initial.gridW,
          gridH: initial.gridH,
          sortOrder: initial.sortOrder,
        })
      } else {
        await api.createFolder({
          title,
          imagePath,
          gridX: 0,
          gridY: 0,
          gridW: 2,
          gridH: 2,
        })
      }
      onSaved()
      onClose()
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Save failed')
    } finally {
      setBusy(false)
    }
  }

  return (
    <div className="modal-backdrop" onClick={onClose}>
      <div className="modal" onClick={(e) => e.stopPropagation()}>
        <h2>{initial ? 'Edit folder' : 'Add folder'}</h2>
        <form className="form-grid" onSubmit={submit}>
          <label>
            Title
            <input value={title} onChange={(e) => setTitle(e.target.value)} required />
          </label>

          <FileBrowseButton
            label="Cover image (optional)"
            buttonText="Browse"
            accept="image/*,.svg,.gif"
            disabled={busy}
            onFile={(file) => void uploadImage(file)}
          />

          {imagePath && (
            <div className="icon-preview-row">
              <div className="service-icon" style={{ width: 64 }}>
                <img src={imagePath} alt="" />
              </div>
              <button type="button" className="btn danger" disabled={busy} onClick={() => void removeImage()}>
                Remove image
              </button>
            </div>
          )}

          {error && <div className="error">{error}</div>}
          <div className="modal-actions">
            <button type="button" className="btn" onClick={onClose}>
              Cancel
            </button>
            <button type="submit" className="btn primary" disabled={busy}>
              Save
            </button>
          </div>
        </form>
      </div>
    </div>
  )
}
