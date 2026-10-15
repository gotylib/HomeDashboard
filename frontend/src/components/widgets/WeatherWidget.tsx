import { useEffect, useState } from 'react'
import type { WeatherConfig } from '../../types'

type Props = {
  config: WeatherConfig
}

type WeatherState = {
  temp: number
  wind: number
  condition: string
  city: string
}

export function WeatherWidget({ config }: Props) {
  const [state, setState] = useState<WeatherState | null>(null)
  const [error, setError] = useState<string | null>(null)

  useEffect(() => {
    let cancelled = false

    async function load() {
      try {
        setError(null)
        const params = new URLSearchParams()
        if (config.latitude != null && config.longitude != null) {
          params.set('lat', String(config.latitude))
          params.set('lon', String(config.longitude))
        }
        if (config.city?.trim()) params.set('city', config.city.trim())

        const res = await fetch(`/api/weather?${params.toString()}`, { credentials: 'include' })
        if (!res.ok) {
          let message = 'Weather request failed'
          try {
            const body = (await res.json()) as { message?: string }
            if (body.message) message = body.message
          } catch {
            /* ignore */
          }
          throw new Error(message)
        }

        const data = (await res.json()) as {
          city: string
          temperatureC: number
          windKmh: number
          condition: string
        }

        if (cancelled) return
        setState({
          temp: data.temperatureC,
          wind: data.windKmh,
          condition: data.condition,
          city: data.city,
        })
      } catch (e) {
        if (!cancelled) setError(e instanceof Error ? e.message : 'Failed to load weather')
      }
    }

    void load()
    const id = window.setInterval(load, 10 * 60 * 1000)
    return () => {
      cancelled = true
      window.clearInterval(id)
    }
  }, [config.city, config.latitude, config.longitude])

  return (
    <div>
      <div className="widget-label">Weather</div>
      {error && <div className="error">{error}</div>}
      {!error && !state && <div className="weather-meta">Loading…</div>}
      {state && (
        <>
          <div className="weather-temp">{Math.round(state.temp)}°</div>
          <div className="weather-meta">
            {state.city} · {state.condition} · wind {Math.round(state.wind)} km/h
          </div>
        </>
      )}
    </div>
  )
}
