import { useEffect, useState } from 'react'
import type { ClockConfig } from '../../types'

type Props = {
  config: ClockConfig
}

export function ClockWidget({ config }: Props) {
  const [now, setNow] = useState(() => new Date())

  useEffect(() => {
    const id = window.setInterval(() => setNow(new Date()), 1000)
    return () => window.clearInterval(id)
  }, [])

  const timeOpts: Intl.DateTimeFormatOptions = {
    hour: '2-digit',
    minute: '2-digit',
    second: config.showSeconds === false ? undefined : '2-digit',
    hour12: false,
    timeZone: config.timezone || undefined,
  }

  const dateOpts: Intl.DateTimeFormatOptions = {
    weekday: 'long',
    year: 'numeric',
    month: 'long',
    day: 'numeric',
    timeZone: config.timezone || undefined,
  }

  return (
    <div>
      <div className="widget-label">Clock</div>
      <div className="clock-time">{now.toLocaleTimeString(undefined, timeOpts)}</div>
      <div className="clock-date">{now.toLocaleDateString(undefined, dateOpts)}</div>
    </div>
  )
}
