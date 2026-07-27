import type { FolderItem } from '../types'

type Props = {
  folder: FolderItem
  editing: boolean
  onOpen: () => void
  onEdit?: () => void
  onDelete?: () => void
}

function previewLetter(title: string) {
  return title.trim().charAt(0).toUpperCase() || '?'
}

export function FolderTile({ folder, editing, onOpen, onEdit, onDelete }: Props) {
  return (
    <div className={`tile folder-tile${editing ? ' editing' : ''}`}>
      {editing && (
        <div className="tile-actions">
          <button type="button" className="icon-btn drag-handle" title="Drag">
            ⠿
          </button>
          <button type="button" className="icon-btn" onClick={onOpen} title="Open folder">
            ↗
          </button>
          <button type="button" className="icon-btn" onClick={onEdit} title="Edit">
            ✎
          </button>
          <button type="button" className="icon-btn" onClick={onDelete} title="Delete">
            ×
          </button>
        </div>
      )}

      <button type="button" className="folder-open-hit" onClick={onOpen} title={`Open ${folder.title}`}>
        <div className="folder-preview">
          {folder.imagePath ? (
            <img className="folder-cover" src={folder.imagePath} alt="" />
          ) : (
            <div className="folder-preview-grid">
              {(folder.preview.length > 0
                ? folder.preview
                : [{ id: 'empty', title: folder.title, imagePath: null }]
              )
                .slice(0, 4)
                .map((item) => (
                  <div key={item.id} className="folder-preview-cell">
                    {item.imagePath ? (
                      <img src={item.imagePath} alt="" />
                    ) : (
                      <span>{previewLetter(item.title)}</span>
                    )}
                  </div>
                ))}
            </div>
          )}
        </div>
        <div className="service-title">{folder.title}</div>
        <div className="folder-count">{folder.serviceCount} apps</div>
      </button>
    </div>
  )
}
