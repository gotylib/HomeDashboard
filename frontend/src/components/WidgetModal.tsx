import { useEffect, useState } from 'react'
import type { FormEvent } from 'react'
import { api } from '../api'
import type { WidgetItem } from '../types'

type Props = {
  open: boolean
  initial?: WidgetItem | null
  onClose: () => void
  onSaved: () => void
}

function defaultSize(type: string) {
  switch (type) {
    case 'search':
      return { gridW: 4, gridH: 2 }
    case 'notes':
      return { gridW: 3, gridH: 3 }
    case 'countdown':
      return { gridW: 3, gridH: 2 }
    default:
      return { gridW: 3, gridH: 2 }
  }
}

export function WidgetModal({ open, initial, onClose, onSaved }: Props) {
  const [type, setType] = useState('clock')
  const [city, setCity] = useState('Moscow')
  const [timezone, setTimezone] = useState(Intl.DateTimeFormat().resolvedOptions().timeZone)
  const [showSeconds, setShowSeconds] = useState(true)
  const [noteTitle, setNoteTitle] = useState('Notes')
  const [noteText, setNoteText] = useState('')
  const [searchEngine, setSearchEngine] = useState('duckduckgo')
  const [searchPlaceholder, setSearchPlaceholder] = useState('Search the web…')
  const [countdownLabel, setCountdownLabel] = useState('Countdown')
  const [countdownTarget, setCountdownTarget] = useState('')
  const [error, setError] = useState<string | null>(null)
  const [busy, setBusy] = useState(false)

  useEffect(() => {
    if (!open) return
    setType(initial?.type ?? 'clock')
    try {
      const cfg = JSON.parse(initial?.configJson || '{}') as Record<string, unknown>
      setCity((cfg.city as string) ?? 'Moscow')
      setTimezone((cfg.timezone as string) ?? Intl.DateTimeFormat().resolvedOptions().timeZone)
      setShowSeconds(cfg.showSeconds !== false)
      setNoteTitle((cfg.title as string) ?? 'Notes')
      setNoteText((cfg.text as string) ?? '')
      setSearchEngine((cfg.engine as string) ?? 'duckduckgo')
      setSearchPlaceholder((cfg.placeholder as string) ?? 'Search the web…')
      setCountdownLabel((cfg.label as string) ?? 'Countdown')
      const rawDate = (cfg.targetDate as string) ?? ''
      setCountdownTarget(rawDate ? rawDate.slice(0, 16) : '')
    } catch {
      /* ignore */
    }
    setError(null)
  }, [open, initial])

  if (!open) return null

  const buildConfig = () => {
    switch (type) {
      case 'weather':
        return JSON.stringify({ city })
      case 'notes':
        return JSON.stringify({ title: noteTitle, text: noteText })
      case 'search':
        return JSON.stringify({ engine: searchEngine, placeholder: searchPlaceholder })
      case 'countdown':
        return JSON.stringify({
          label: countdownLabel,
          targetDate: countdownTarget ? new Date(countdownTarget).toISOString() : '',
        })
      default:
        return JSON.stringify({ timezone, showSeconds })
    }
  }

  const submit = async (e: FormEvent) => {
    e.preventDefault()
    setBusy(true)
    setError(null)
    const configJson = buildConfig()
    const size = defaultSize(type)

    try {
      if (initial) {
        await api.updateWidget(initial.id, {
          type,
          configJson,
          gridX: initial.gridX,
          gridY: initial.gridY,
          gridW: initial.gridW,
          gridH: initial.gridH,
        })
      } else {
        await api.createWidget({
          type,
          configJson,
          gridX: 0,
          gridY: 0,
          ...size,
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
        <h2>{initial ? 'Edit widget' : 'Add widget'}</h2>
        <form className="form-grid" onSubmit={submit}>
          <label>
            Type
            <select value={type} onChange={(e) => setType(e.target.value)} disabled={!!initial}>
              <option value="clock">Clock</option>
              <option value="weather">Weather</option>
              <option value="notes">Notes</option>
              <option value="search">Search</option>
              <option value="countdown">Countdown</option>
            </select>
          </label>

          {type === 'weather' && (
            <label>
              City
              <input value={city} onChange={(e) => setCity(e.target.value)} required />
            </label>
          )}

          {type === 'clock' && (
            <>
              <label>
                Timezone
                <input value={timezone} onChange={(e) => setTimezone(e.target.value)} />
              </label>
              <label className="checkbox-row">
                <input
                  type="checkbox"
                  checked={showSeconds}
                  onChange={(e) => setShowSeconds(e.target.checked)}
                />
                <span>Show seconds</span>
              </label>
            </>
          )}

          {type === 'notes' && (
            <>
              <label>
                Title
                <input value={noteTitle} onChange={(e) => setNoteTitle(e.target.value)} />
              </label>
              <label>
                Text
                <textarea
                  rows={5}
                  value={noteText}
                  onChange={(e) => setNoteText(e.target.value)}
                  placeholder="Write a note…"
                />
              </label>
            </>
          )}

          {type === 'search' && (
            <>
              <label>
                Engine
                <select value={searchEngine} onChange={(e) => setSearchEngine(e.target.value)}>
                  <option value="duckduckgo">DuckDuckGo</option>
                  <option value="google">Google</option>
                  <option value="bing">Bing</option>
                </select>
              </label>
              <label>
                Placeholder
                <input value={searchPlaceholder} onChange={(e) => setSearchPlaceholder(e.target.value)} />
              </label>
            </>
          )}

          {type === 'countdown' && (
            <>
              <label>
                Label
                <input value={countdownLabel} onChange={(e) => setCountdownLabel(e.target.value)} />
              </label>
              <label>
                Target date & time
                <input
                  type="datetime-local"
                  value={countdownTarget}
                  onChange={(e) => setCountdownTarget(e.target.value)}
                  required
                />
              </label>
            </>
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
