export type Wallpaper = {
  path: string | null
  type: string
}

export type FolderPreviewItem = {
  id: string
  title: string
  imagePath: string | null
}

export type FolderItem = {
  id: string
  title: string
  imagePath: string | null
  gridX: number
  gridY: number
  gridW: number
  gridH: number
  sortOrder: number
  serviceCount: number
  preview: FolderPreviewItem[]
}

export type ServiceItem = {
  id: string
  title: string
  url: string
  imagePath: string | null
  healthUrl: string | null
  folderId: string | null
  gridX: number
  gridY: number
  gridW: number
  gridH: number
  sortOrder: number
  isUp: boolean | null
  checkedAt: string | null
}

export type WidgetItem = {
  id: string
  type: 'clock' | 'weather' | 'notes' | 'search' | 'countdown' | string
  configJson: string
  gridX: number
  gridY: number
  gridW: number
  gridH: number
}

export type Dashboard = {
  wallpaper: Wallpaper
  folders: FolderItem[]
  services: ServiceItem[]
  widgets: WidgetItem[]
}

export type WeatherConfig = {
  city?: string
  latitude?: number
  longitude?: number
}

export type ClockConfig = {
  timezone?: string
  showSeconds?: boolean
}

export type NotesConfig = {
  title?: string
  text?: string
}

export type SearchConfig = {
  engine?: 'duckduckgo' | 'google' | 'bing' | string
  placeholder?: string
}

export type CountdownConfig = {
  label?: string
  targetDate?: string
}
