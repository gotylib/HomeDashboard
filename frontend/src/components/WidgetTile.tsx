import type {
  WidgetItem,
  ClockConfig,
  WeatherConfig,
  NotesConfig,
  SearchConfig,
  CountdownConfig,
} from '../types'
import { ClockWidget } from './widgets/ClockWidget'
import { WeatherWidget } from './widgets/WeatherWidget'
import { NotesWidget } from './widgets/NotesWidget'
import { SearchWidget } from './widgets/SearchWidget'
import { CountdownWidget } from './widgets/CountdownWidget'

type Props = {
  widget: WidgetItem
  editing: boolean
  onEdit?: () => void
  onDelete?: () => void
}

function parseConfig<T>(json: string): T {
  try {
    return JSON.parse(json || '{}') as T
  } catch {
    return {} as T
  }
}

export function WidgetTile({ widget, editing, onEdit, onDelete }: Props) {
  return (
    <div className={`tile widget-tile${editing ? ' editing' : ''}`}>
      {editing && (
        <div className="tile-actions">
          <button type="button" className="icon-btn" onClick={onEdit} title="Edit">
            ✎
          </button>
          <button type="button" className="icon-btn" onClick={onDelete} title="Delete">
            ×
          </button>
        </div>
      )}

      {widget.type === 'clock' && <ClockWidget config={parseConfig<ClockConfig>(widget.configJson)} />}
      {widget.type === 'weather' && (
        <WeatherWidget config={parseConfig<WeatherConfig>(widget.configJson)} />
      )}
      {widget.type === 'notes' && <NotesWidget config={parseConfig<NotesConfig>(widget.configJson)} />}
      {widget.type === 'search' && <SearchWidget config={parseConfig<SearchConfig>(widget.configJson)} />}
      {widget.type === 'countdown' && (
        <CountdownWidget config={parseConfig<CountdownConfig>(widget.configJson)} />
      )}
      {!['clock', 'weather', 'notes', 'search', 'countdown'].includes(widget.type) && (
        <div className="weather-meta">Unknown widget: {widget.type}</div>
      )}
    </div>
  )
}
