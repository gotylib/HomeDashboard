import { useEffect, useMemo, useState } from 'react'
import type { CountdownConfig } from '../../types'

type Props = {
  config: CountdownConfig
}

type Parts = {
  expired: boolean
  days: number
  hours: number
  minutes: number
  seconds: number
}

function diff(targetIso: string): Parts {
  const target = new Date(targetIso).getTime()
  const now = Date.now()
  const ms = target - now
  if (Number.isNaN(target) || ms <= 0) {
    return { expired: true, days: 0, hours: 0, minutes: 0, seconds: 0 }
  }
  const totalSec = Math.floor(ms / 1000)
  return {
    expired: false,
    days: Math.floor(totalSec / 86400),
    hours: Math.floor((totalSec % 86400) / 3600),
    minutes: Math.floor((totalSec % 3600) / 60),
    seconds: totalSec % 60,
  }
}

export function CountdownWidget({ config }: Props) {
  const target = config.targetDate || ''
  const [parts, setParts] = useState<Parts>(() => (target ? diff(target) : {
    expired: true, days: 0, hours: 0, minutes: 0, seconds: 0,
  }))

  useEffect(() => {
    if (!target) return
    const tick = () => setParts(diff(target))
    tick()
    const id = window.setInterval(tick, 1000)
    return () => window.clearInterval(id)
  }, [target])

  const label = useMemo(() => config.label?.trim() || 'Countdown', [config.label])

  if (!target) {
    return (
      <div>
        <div className="widget-label">{label}</div>
        <div className="weather-meta">Set a target date</div>
      </div>
    )
  }

  return (
    <div className="countdown-widget">
      <div className="widget-label">{label}</div>
      {parts.expired ? (
        <div className="countdown-done">Time reached</div>
      ) : (
        <div className="countdown-grid">
          <div><strong>{parts.days}</strong><span>days</span></div>
          <div><strong>{parts.hours}</strong><span>hrs</span></div>
          <div><strong>{parts.minutes}</strong><span>min</span></div>
          <div><strong>{parts.seconds}</strong><span>sec</span></div>
        </div>
      )}
    </div>
  )
}
