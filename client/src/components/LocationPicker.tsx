import { useEffect, useMemo, useRef, useState } from 'react'
import { MapContainer, Marker, TileLayer, useMap, useMapEvents } from 'react-leaflet'
import type { LeafletEventHandlerFnMap, Marker as LeafletMarker } from 'leaflet'
import { reverseGeocode, searchAddress, type Place } from '../api/geocoding'
import { ISRAEL_CENTER } from '../browserLocation'
import { AutoResize } from '../map/AutoResize'
import { TILE_ATTRIBUTION, TILE_URL } from '../map/leaflet'
import { t } from '../appText'
import type { LatLng } from '../types'

interface Props {
  value: LatLng | null
  address: string
  error?: string
  onChange: (location: LatLng, address: string | null) => void
  onAddress: (address: string) => void
}

/**
 * Picks the ad's location: search an address, click the map, or drag the pin.
 * After the pin settles, the address is looked up from it (Nominatim allows ~1 request a second).
 */
export function LocationPicker({ value, address, error, onChange, onAddress }: Props) {
  const [query, setQuery] = useState('')
  const [results, setResults] = useState<Place[] | null>(null)
  const [busy, setBusy] = useState(false)
  const [message, setMessage] = useState<string | null>(null)
  const lookupAfterMove = useRef(false)

  // Reverse lookup of the address, one second after the pin last moved by click or drag.
  useEffect(() => {
    if (!value || !lookupAfterMove.current) return
    const controller = new AbortController()
    const timer = setTimeout(() => {
      reverseGeocode(value, controller.signal)
        .then((label) => label && onAddress(label))
        .catch(() => {})
    }, 1000)
    return () => {
      clearTimeout(timer)
      controller.abort()
    }
    // eslint-disable-next-line react-hooks/exhaustive-deps -- only when the pin moves
  }, [value])

  const moveTo = (point: LatLng, label: string | null, lookup: boolean) => {
    lookupAfterMove.current = lookup
    onChange(point, label)
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

  const choose = (place: Place) => {
    setResults(null)
    setQuery('')
    moveTo({ lat: place.lat, lng: place.lng }, place.label, false)
  }

  return (
    <div className={error ? 'picker invalid' : 'picker'}>
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

      <MapContainer
        center={value ? [value.lat, value.lng] : [ISRAEL_CENTER.lat, ISRAEL_CENTER.lng]}
        zoom={value ? 15 : 7}
        className="picker-map"
      >
        <TileLayer url={TILE_URL} attribution={TILE_ATTRIBUTION} />
        <AutoResize />
        <ClickToPlace onPlace={(p) => moveTo(p, null, true)} />
        <Recenter point={value} />
        {value && <DraggablePin point={value} onMove={(p) => moveTo(p, null, true)} />}
      </MapContainer>

      <p className="hint">{t.pickOnMap}</p>
      {value && (
        <p className="picked">
          📍 {address || `${t.pinnedAt} ${value.lat.toFixed(5)}, ${value.lng.toFixed(5)}`}
        </p>
      )}
      {error && <p className="field-error">{error}</p>}
    </div>
  )
}

function ClickToPlace({ onPlace }: { onPlace: (p: LatLng) => void }) {
  useMapEvents({ click: (e) => onPlace({ lat: e.latlng.lat, lng: e.latlng.lng }) })
  return null
}

function Recenter({ point }: { point: LatLng | null }) {
  const map = useMap()
  useEffect(() => {
    // Zoom in to street level so the pin can be fine-tuned, or pan if it's off-screen.
    if (point && (map.getZoom() < 14 || !map.getBounds().pad(-0.1).contains([point.lat, point.lng]))) {
      map.setView([point.lat, point.lng], Math.max(map.getZoom(), 15))
    }
  }, [map, point])
  return null
}

function DraggablePin({ point, onMove }: { point: LatLng; onMove: (p: LatLng) => void }) {
  const ref = useRef<LeafletMarker>(null)
  const handlers = useMemo<LeafletEventHandlerFnMap>(
    () => ({
      dragend: () => {
        const ll = ref.current?.getLatLng()
        if (ll) onMove({ lat: ll.lat, lng: ll.lng })
      },
    }),
    [onMove],
  )
  return <Marker position={[point.lat, point.lng]} draggable eventHandlers={handlers} ref={ref} />
}
