import { useEffect } from 'react'
import { Circle, CircleMarker, MapContainer, TileLayer, useMapEvents } from 'react-leaflet'
import { MapAutoResize } from '../map/MapAutoResize'
import { AdPinsLayer } from '../map/AdPinsLayer'
import { FlyTo } from '../map/FlyTo'
import { TILE_ATTRIBUTION, TILE_URL } from '../map/leafletSetup'
import { ISRAEL_CENTER } from '../browserLocation'
import { t } from '../appText'
import type { AdPin, Bounds, LatLng, PinsResult } from '../types'
import { MapPlaceSearch } from './MapPlaceSearch'

interface Props {
  pins: PinsResult | null
  selectedId: string | null
  focus: LatLng | null
  place: LatLng | null // the centre of the distance filter: a pinned place or the browser's location
  placeLabel: string | null
  radiusKm: number | null
  onSelect: (id: string) => void
  onBoundsChange: (bounds: Bounds) => void
  onPlace: (point: LatLng, label: string | null) => void
  onClearPlace: () => void
}

/**
 * The map of every ad that matches the filters (within the visible area). Searching an address
 * or clicking the map pins your place, the centre the distance filter measures from.
 */
export function AdsMap({ pins, selectedId, focus, place, placeLabel, radiusKm, onSelect, onBoundsChange, onPlace, onClearPlace }: Props) {
  return (
    <div className="map-wrap">
      <MapContainer center={[ISRAEL_CENTER.lat, ISRAEL_CENTER.lng]} zoom={8} className="map" worldCopyJump>
        <TileLayer url={TILE_URL} attribution={TILE_ATTRIBUTION} />
        <MapAutoResize />
        <BoundsReporter onChange={onBoundsChange} />
        <FlyTo point={focus} />
        <ClickToPlace onPlace={(point) => onPlace(point, null)} />
        <AdPinsLayer pins={pins?.pins ?? EMPTY} selectedId={selectedId} onSelect={onSelect} />
        {place && (
          <>
            {radiusKm && (
              <Circle center={[place.lat, place.lng]} radius={radiusKm * 1000} pathOptions={{ className: 'radius' }} />
            )}
            <CircleMarker center={[place.lat, place.lng]} radius={8} pathOptions={{ className: 'me' }} />
          </>
        )}
      </MapContainer>
      <MapPlaceSearch pinned={place !== null} label={placeLabel} onPlace={onPlace} onClear={onClearPlace} />
      {pins?.truncated && <p className="map-note">{t.pinsTruncated(pins.pins.length, pins.total)}</p>}
    </div>
  )
}

const EMPTY: AdPin[] = []

/** A click on the map (not on an ad's pin, which opens the ad) pins your place there. */
function ClickToPlace({ onPlace }: { onPlace: (point: LatLng) => void }) {
  useMapEvents({ click: (e) => onPlace({ lat: e.latlng.lat, lng: e.latlng.lng }) })
  return null
}

function BoundsReporter({ onChange }: { onChange: (b: Bounds) => void }) {
  const map = useMapEvents({ moveend: () => report() })
  const report = () => {
    const b = map.getBounds()
    onChange({ north: b.getNorth(), south: b.getSouth(), east: b.getEast(), west: b.getWest() })
  }
  useEffect(() => {
    report()
    // eslint-disable-next-line react-hooks/exhaustive-deps -- report once when the map first appears
  }, [])
  return null
}
