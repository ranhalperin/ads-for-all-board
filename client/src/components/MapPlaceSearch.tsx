import { useState } from 'react'
import { searchAddress, type Place } from '../api/geocoding'
import { t } from '../appText'
import type { LatLng } from '../types'

interface Props {
  pinned: boolean // a place is set on the map
  label: string | null // its address, once known
  onPlace: (point: LatLng, label: string | null) => void
  onClear: () => void
}

/** The box over the board's map: search an address to pin your place, or remove the pin. */
export function MapPlaceSearch({ pinned, label, onPlace, onClear }: Props) {
  const [query, setQuery] = useState('')
  const [results, setResults] = useState<Place[] | null>(null)
  const [busy, setBusy] = useState(false)
  const [message, setMessage] = useState<string | null>(null)

  const choose = (place: Place) => {
    setResults(null)
    setQuery('')
    onPlace({ lat: place.lat, lng: place.lng }, place.label)
  }

  const search = async () => {
    const q = query.trim()
    if (!q) return
    setBusy(true)
    setMessage(null)
    try {
      const found = await searchAddress(q)
      setResults(found)
      if (found.length === 0) setMessage(t.noAddressFound)
      else if (found.length === 1) choose(found[0])
    } catch {
      setMessage(t.errGeneric)
    } finally {
      setBusy(false)
    }
  }

  return (
    <div className="place-search">
      <div className="picker-search">
        <input
          type="search"
          placeholder={t.addressSearch}
          aria-label={t.addressSearch}
          value={query}
          onChange={(e) => setQuery(e.target.value)}
          onKeyDown={(e) => {
            if (e.key === 'Enter') {
              e.preventDefault()
              void search()
            }
          }}
        />
        <button type="button" onClick={() => void search()} disabled={busy}>{t.addressSearchButton}</button>
      </div>

      {results && results.length > 1 && (
        <ul className="picker-results">
          {results.map((r) => (
            <li key={`${r.lat},${r.lng}`}>
              <button type="button" onClick={() => choose(r)}>{r.label}</button>
            </li>
          ))}
        </ul>
      )}
      {message && <p className="field-error">{message}</p>}

      {pinned ? (
        <p className="place-pinned">
          <span>📍 {label ?? t.pinnedPlace}</span>
          <button type="button" className="link" onClick={onClear}>{t.removePin}</button>
        </p>
      ) : (
        <p className="hint">{t.pinHint}</p>
      )}
    </div>
  )
}
