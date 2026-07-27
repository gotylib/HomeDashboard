import type { NotesConfig } from '../../types'

type Props = {
  config: NotesConfig
}

export function NotesWidget({ config }: Props) {
  return (
    <div className="notes-widget">
      <div className="widget-label">{config.title?.trim() || 'Notes'}</div>
      <div className="notes-body">{config.text?.trim() || 'Empty note'}</div>
    </div>
  )
}
