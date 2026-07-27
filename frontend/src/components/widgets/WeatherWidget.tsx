import { useEffect, useState } from 'react'
import type { WeatherConfig } from '../../types'

type Props = {
  config: WeatherConfig
}

type WeatherState = {
  temp: number
  wind: number
  code: number
  city: string
}

const WMO: Record<number, string> = {
  0: 'Clear',
  1: 'Mainly clear',
  2: 'Partly cloudy',
  3: 'Overcast',
  45: 'Fog',
  48: 'Fog',
  51: 'Drizzle',
  61: 'Rain',
  71: 'Snow',
  80: 'Showers',
  95: 'Thunderstorm',
}

async function resolveCoords(config: WeatherConfig): Promise<{ lat: number; lon: number; city: string }> {
  if (config.latitude != null && config.longitude != null) {
    return { lat: config.latitude, lon: config.longitude, city: config.city || 'Custom' }
  }

  const city = config.city?.trim() || 'Moscow'
  const geo = await fetch(
    `https://geocoding-api.open-meteo.com/v1/search?name=${encodeURIComponent(city)}&count=1&language=en&format=json`,
  )
  if (!geo.ok) throw new Error('Geocoding failed')
  const data = await geo.json()
  const hit = data.results?.[0]
  if (!hit) throw new Error(`City not found: ${city}`)
  return { lat: hit.latitude, lon: hit.longitude, city: hit.name }
}

export function WeatherWidget({ config }: Props) {
  const [state, setState] = useState<WeatherState | null>(null)
  const [error, setError] = useState<string | null>(null)

  useEffect(() => {
    let cancelled = false

    async function load() {
      try {
        setError(null)
        const { lat, lon, city } = await resolveCoords(config)
        const res = await fetch(
          `https://api.open-meteo.com/v1/forecast?latitude=${lat}&longitude=${lon}&current=temperature_2m,weather_code,wind_speed_10m`,
        )
        if (!res.ok) throw new Error('Weather request failed')
        const data = await res.json()
        if (cancelled) return
        setState({
          temp: data.current.temperature_2m,
          wind: data.current.wind_speed_10m,
          code: data.current.weather_code,
          city,
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
            {state.city} · {WMO[state.code] ?? 'Weather'} · wind {Math.round(state.wind)} km/h
          </div>
        </>
      )}
    </div>
  )
}
