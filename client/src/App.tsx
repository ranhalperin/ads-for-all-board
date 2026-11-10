import { useCallback, useEffect, useMemo, useState } from 'react'
import { boundsParams, getPins, searchAds, toQueryParams } from './api/ads'
import { errorMessage, isCanceled } from './api/http'
import { AdPanel } from './components/AdPanel'
import { AdsList } from './components/AdsList'
import { AdsMap } from './components/AdsMap'
import { reverseGeocode } from './api/geocoding'
import { FiltersBar } from './components/FiltersBar'
import { getCurrentLocation, locationErrorMessage } from './browserLocation'
import { useDebounced } from './hooks'
import { t } from './appText'
import { DEFAULT_FILTERS, type Ad, type Bounds, type Filters, type LatLng, type PagedResult, type PanelState, type PinsResult } from './types'

/** The distance a pinned place starts with, when no distance is chosen yet. */
const PIN_RADIUS_KM = 10

/**
 * The whole app on one screen: filters on top, the list and the map side by side,
 * and a side panel that shows, creates or edits an ad. Nothing reloads the page.
 */
export default function App() {
  const [filters, setFilters] = useState<Filters>(DEFAULT_FILTERS)
  const [page, setPage] = useState(1)
  // The centre of the distance filter: a place pinned on the map, or the browser's location.
  const [myLocation, setMyLocation] = useState<LatLng | null>(null)
  const [placeLabel, setPlaceLabel] = useState<string | null>(null)
  const [locating, setLocating] = useState(false)
  const [locationError, setLocationError] = useState<string | null>(null)
  const [bounds, setBounds] = useState<Bounds | null>(null)
  const [focus, setFocus] = useState<LatLng | null>(null)
  const [panel, setPanel] = useState<PanelState>({ mode: 'closed' })
  const [askDelete, setAskDelete] = useState(false)
  const [refreshKey, setRefreshKey] = useState(0)

  const [result, setResult] = useState<PagedResult<Ad> | null>(null)
  const [pins, setPins] = useState<PinsResult | null>(null)
  const [loading, setLoading] = useState(false)
  const [error, setError] = useState<string | null>(null)

  // Typing in the search box and moving the map settle before they hit the API.
  const debouncedFilters = useDebounced(filters, 300)
  const debouncedBounds = useDebounced(bounds, 400)
  const listParams = useMemo(() => toQueryParams(debouncedFilters, myLocation, page), [debouncedFilters, myLocation, page])
  // The map gets the same filters, limited to the area in view.
  const pinParams = useMemo(
    () => (debouncedBounds ? { ...toQueryParams(debouncedFilters, myLocation), ...boundsParams(debouncedBounds) } : null),
    [debouncedFilters, myLocation, debouncedBounds],
  )

  // The list: one page of matching ads.
  const listKey = JSON.stringify(listParams)
  useEffect(() => {
    const controller = new AbortController()
    setLoading(true)
    searchAds(listParams, controller.signal)
      .then((r) => {
        setResult(r)
        setError(null)
      })
      .catch((err) => !isCanceled(err) && setError(errorMessage(err)))
      .finally(() => !controller.signal.aborted && setLoading(false))
    return () => controller.abort()
    // eslint-disable-next-line react-hooks/exhaustive-deps -- listKey stands for listParams
  }, [listKey, refreshKey])

  // The map: light pins for the matching ads in the visible area.
  const pinKey = JSON.stringify(pinParams)
  useEffect(() => {
    if (!pinParams) return
    const controller = new AbortController()
    getPins(pinParams, controller.signal)
      .then(setPins)
      .catch(() => {}) // the list shows errors; the map just keeps its last pins
    return () => controller.abort()
    // eslint-disable-next-line react-hooks/exhaustive-deps -- pinKey stands for pinParams
  }, [pinKey, refreshKey])

  // A place set without its address (a map click, the browser's location): look the address up.
  // The short wait lets a few quick clicks settle into one lookup (Nominatim allows ~1 a second).
  useEffect(() => {
    if (!myLocation || placeLabel !== null) return
    const controller = new AbortController()
    const timer = setTimeout(() => {
      reverseGeocode(myLocation, controller.signal)
        .then((label) => label && setPlaceLabel(label))
        .catch(() => {})
    }, 600)
    return () => {
      clearTimeout(timer)
      controller.abort()
    }
  }, [myLocation, placeLabel])

  const updateFilters = useCallback((patch: Partial<Filters>) => {
    setFilters((f) => ({ ...f, ...patch }))
    setPage(1)
  }, [])

  /** Pin your place (address search or map click): the distance filter measures from it. */
  const placeAt = (point: LatLng, label: string | null) => {
    setLocationError(null)
    setMyLocation(point)
    setPlaceLabel(label)
    setFocus(point)
    if (filters.radiusKm === null) updateFilters({ radiusKm: PIN_RADIUS_KM })
  }

  const clearPlace = () => {
    setMyLocation(null)
    setPlaceLabel(null)
    updateFilters({ radiusKm: null })
  }

  /** Picking a distance needs a place: the pinned one, or else the browser's location (asked once). "Any" turns the filter off. */
  const changeDistance = async (km: number | null) => {
    setLocationError(null)
    if (km === null || myLocation) {
      updateFilters({ radiusKm: km })
      if (km !== null && myLocation) setFocus(myLocation)
      return
    }
    setLocating(true)
    try {
      const here = await getCurrentLocation()
      setMyLocation(here)
      setPlaceLabel(null)
      setFocus(here)
      updateFilters({ radiusKm: km })
    } catch (err) {
      setLocationError(locationErrorMessage(err))
    } finally {
      setLocating(false)
    }
  }

  const openAd = (id: string, at?: LatLng) => {
    setAskDelete(false)
    setPanel({ mode: 'view', id })
    if (at) setFocus(at)
  }

  const selectedId = panel.mode === 'view' ? panel.id : panel.mode === 'edit' ? panel.ad.id : null
  const filtered = JSON.stringify(filters) !== JSON.stringify(DEFAULT_FILTERS)

  return (
    <div className={panel.mode === 'closed' ? 'app' : 'app with-panel'}>
      <header className="header">
        <h1>📌 {t.appName}</h1>
        <button type="button" className="primary" onClick={() => { setAskDelete(false); setPanel({ mode: 'new' }) }}>
          + {t.newAd}
        </button>
      </header>

      <FiltersBar
        filters={filters}
        locating={locating}
        locationError={locationError}
        onChange={updateFilters}
        onDistance={(km) => void changeDistance(km)}
        onClear={() => { setFilters(DEFAULT_FILTERS); setPage(1); setLocationError(null); setMyLocation(null); setPlaceLabel(null) }}
      />

      <main className="main">
        <AdsList
          result={result}
          loading={loading}
          error={error}
          filtered={filtered}
          selectedId={selectedId}
          onSelect={(ad) => openAd(ad.id, { lat: ad.latitude, lng: ad.longitude })}
          onEdit={(ad) => setPanel({ mode: 'edit', ad })}
          onDelete={(ad) => { openAd(ad.id, { lat: ad.latitude, lng: ad.longitude }); setAskDelete(true) }}
          onPage={setPage}
        />

        <AdsMap
          pins={pins}
          selectedId={selectedId}
          focus={focus}
          place={myLocation}
          placeLabel={placeLabel}
          radiusKm={filters.radiusKm}
          onSelect={(id) => openAd(id)}
          onBoundsChange={setBounds}
          onPlace={placeAt}
          onClearPlace={clearPlace}
        />

        <AdPanel
          key={selectedId ?? panel.mode}
          state={panel}
          askDelete={askDelete}
          onClose={() => setPanel({ mode: 'closed' })}
          onEdit={(ad) => setPanel({ mode: 'edit', ad })}
          onSaved={(ad) => {
            openAd(ad.id, { lat: ad.latitude, lng: ad.longitude })
            setRefreshKey((k) => k + 1)
          }}
          onDeleted={() => {
            setPanel({ mode: 'closed' })
            setAskDelete(false)
            setRefreshKey((k) => k + 1)
          }}
          onAskDelete={setAskDelete}
        />
      </main>
    </div>
  )
}
