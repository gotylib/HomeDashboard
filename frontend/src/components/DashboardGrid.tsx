import GridLayout, { type Layout } from 'react-grid-layout'
import 'react-grid-layout/css/styles.css'
import 'react-resizable/css/styles.css'
import type { FolderItem, ServiceItem, WidgetItem } from '../types'
import { ServiceTile } from './ServiceTile'
import { WidgetTile } from './WidgetTile'
import { FolderTile } from './FolderTile'

export type GridItem =
  | { kind: 'service'; data: ServiceItem }
  | { kind: 'widget'; data: WidgetItem }
  | { kind: 'folder'; data: FolderItem }

type Props = {
  items: GridItem[]
  editing: boolean
  width: number
  onLayoutChange: (layout: Layout[]) => void
  onEditService: (item: ServiceItem) => void
  onDeleteService: (item: ServiceItem) => void
  onEditWidget: (item: WidgetItem) => void
  onDeleteWidget: (item: WidgetItem) => void
  onOpenFolder: (item: FolderItem) => void
  onEditFolder: (item: FolderItem) => void
  onDeleteFolder: (item: FolderItem) => void
}

export function DashboardGrid({
  items,
  editing,
  width,
  onLayoutChange,
  onEditService,
  onDeleteService,
  onEditWidget,
  onDeleteWidget,
  onOpenFolder,
  onEditFolder,
  onDeleteFolder,
}: Props) {
  const layout: Layout[] = items.map((item) => ({
    i: `${item.kind}:${item.data.id}`,
    x: item.data.gridX,
    y: item.data.gridY,
    w: item.data.gridW,
    h: item.data.gridH,
    minW: 1,
    minH: 1,
  }))

  if (items.length === 0) {
    return (
      <div className="empty-hint">
        {editing
          ? 'Add a service, folder or widget from the toolbar to start building your home page.'
          : 'No tiles yet. Sign in to add services, folders and widgets.'}
      </div>
    )
  }

  return (
    <GridLayout
      className="layout"
      layout={layout}
      cols={12}
      rowHeight={72}
      width={Math.max(width, 320)}
      margin={[14, 14]}
      containerPadding={[0, 0]}
      isDraggable={editing}
      isResizable={editing}
      compactType={null}
      preventCollision={false}
      onLayoutChange={onLayoutChange}
      draggableCancel=".icon-btn,button,input,a"
    >
      {items.map((item) => (
        <div key={`${item.kind}:${item.data.id}`}>
          {item.kind === 'service' && (
            <ServiceTile
              service={item.data}
              editing={editing}
              onEdit={() => onEditService(item.data)}
              onDelete={() => onDeleteService(item.data)}
            />
          )}
          {item.kind === 'widget' && (
            <WidgetTile
              widget={item.data}
              editing={editing}
              onEdit={() => onEditWidget(item.data)}
              onDelete={() => onDeleteWidget(item.data)}
            />
          )}
          {item.kind === 'folder' && (
            <FolderTile
              folder={item.data}
              editing={editing}
              onOpen={() => onOpenFolder(item.data)}
              onEdit={() => onEditFolder(item.data)}
              onDelete={() => onDeleteFolder(item.data)}
            />
          )}
        </div>
      ))}
    </GridLayout>
  )
}
