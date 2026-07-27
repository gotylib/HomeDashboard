import { useState } from 'react'
import type { FormEvent } from 'react'
import type { SearchConfig } from '../../types'

type Props = {
  config: SearchConfig
}

const ENGINES: Record<string, string> = {
  duckduckgo: 'https://duckduckgo.com/?q=',
  google: 'https://www.google.com/search?q=',
  bing: 'https://www.bing.com/search?q=',
}

export function SearchWidget({ config }: Props) {
  const [query, setQuery] = useState('')
  const engine = (config.engine || 'duckduckgo').toLowerCase()
  const base = ENGINES[engine] ?? ENGINES.duckduckgo

  const submit = (e: FormEvent) => {
    e.preventDefault()
    const q = query.trim()
    if (!q) return
    window.open(`${base}${encodeURIComponent(q)}`, '_blank', 'noopener,noreferrer')
  }

  return (
    <div className="search-widget">
      <div className="widget-label">Search</div>
      <form className="search-form" onSubmit={submit}>
        <input
          value={query}
          onChange={(e) => setQuery(e.target.value)}
          placeholder={config.placeholder || `Search ${engine}…`}
          aria-label="Search query"
        />
        <button type="submit" className="btn primary">
          Go
        </button>
      </form>
    </div>
  )
}
