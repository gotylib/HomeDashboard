import type { FolderItem, ServiceItem } from '../types'
import { ServiceTile } from './ServiceTile'

type Props = {
  folder: FolderItem
  services: ServiceItem[]
  editing: boolean
  onClose: () => void
  onEditService: (item: ServiceItem) => void
  onDeleteService: (item: ServiceItem) => void
  onAddService: () => void
  onEditFolder: () => void
}

export function FolderView({
  folder,
  services,
  editing,
  onClose,
  onEditService,
  onDeleteService,
  onAddService,
  onEditFolder,
}: Props) {
  return (
    <div className="folder-view-backdrop" onClick={onClose}>
      <div className="folder-view" onClick={(e) => e.stopPropagation()}>
        <div className="folder-view-header">
          <button type="button" className="btn" onClick={onClose}>
            ← Back
          </button>
          <h2>{folder.title}</h2>
          <div className="folder-view-actions">
            {editing && (
              <>
                <button type="button" className="btn" onClick={onEditFolder}>
                  Edit folder
                </button>
                <button type="button" className="btn primary" onClick={onAddService}>
                  + Service
                </button>
              </>
            )}
          </div>
        </div>

        {services.length === 0 ? (
          <div className="empty-hint">
            {editing
              ? 'This folder is empty. Add a service and choose this folder in the form.'
              : 'This folder is empty.'}
          </div>
        ) : (
          <div className="folder-view-grid">
            {services.map((service) => (
              <div key={service.id} className="folder-view-item">
                <ServiceTile
                  service={service}
                  editing={editing}
                  onEdit={() => onEditService(service)}
                  onDelete={() => onDeleteService(service)}
                />
              </div>
            ))}
          </div>
        )}
      </div>
    </div>
  )
}
