import type { ServiceItem } from '../types'

type Props = {
  service: ServiceItem
  editing: boolean
  onEdit?: () => void
  onDelete?: () => void
}

function initials(title: string) {
  return title
    .split(/\s+/)
    .filter(Boolean)
    .slice(0, 2)
    .map((p) => p[0]?.toUpperCase() ?? '')
    .join('') || '?'
}

export function ServiceTile({ service, editing, onEdit, onDelete }: Props) {
  const healthClass =
    service.isUp === true ? 'up' : service.isUp === false ? 'down' : 'unknown'

  const open = () => {
    if (editing) return
    window.open(service.url, '_blank', 'noopener,noreferrer')
  }

  return (
    <div
      className={`tile service-tile${editing ? ' editing' : ''}`}
      onClick={open}
      role={editing ? undefined : 'link'}
      tabIndex={editing ? -1 : 0}
      onKeyDown={(e) => {
        if (!editing && (e.key === 'Enter' || e.key === ' ')) open()
      }}
    >
      <span
        className={`health-dot ${healthClass}`}
        title={
          service.isUp === true
            ? 'Online'
            : service.isUp === false
              ? 'Offline'
              : 'Status unknown'
        }
      />

      {editing && (
        <div className="tile-actions" onClick={(e) => e.stopPropagation()}>
          <button type="button" className="icon-btn" onClick={onEdit} title="Edit">
            ✎
          </button>
          <button type="button" className="icon-btn" onClick={onDelete} title="Delete">
            ×
          </button>
        </div>
      )}

      <div className="service-icon">
        {service.imagePath ? (
          <img src={service.imagePath} alt="" />
        ) : (
          <span>{initials(service.title)}</span>
        )}
      </div>
      <div className="service-title">{service.title}</div>
    </div>
  )
}
