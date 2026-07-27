import type { Wallpaper } from '../types'

type Props = {
  wallpaper: Wallpaper
}

export function WallpaperBackground({ wallpaper }: Props) {
  const src = wallpaper.path?.trim() || null
  const type = wallpaper.type ?? 'none'
  const showMedia = !!src && type !== 'none'

  return (
    <div className="wallpaper" aria-hidden>
      {showMedia && type === 'video' && (
        <video key={src} autoPlay muted loop playsInline src={src!} />
      )}
      {showMedia && type !== 'video' && (
        <img key={src} src={src!} alt="" />
      )}
    </div>
  )
}
