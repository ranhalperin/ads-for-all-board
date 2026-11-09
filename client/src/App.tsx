import { useCallback, useEffect, useMemo, useState } from 'react'
import { boundsParams, getPins, searchAds, toQueryParams } from './api/ads'
import { errorMessage, isCanceled } from './api/http'
import { AdPanel } from './components/AdPanel'
import { AdsList } from './components/AdsList'
import { AdsMap } from './components/AdsMap'
import { FiltersBar } from './components/FiltersBar'
import { getCurrentLocation } from './geo'
import { useDebounced } from './hooks'
import { loadMyName, saveMyName } from './myName'
import { t } from './strings'
import { DEFAULT_FILTERS, type Ad, type Bounds, type Filters, type LatLng, type PagedResult, type PanelState, type PinsResult } from './types'

/**
 * The whole app on one screen: filters on top, the list and the map side by side,
 * and a side panel that shows, creates or edits an ad. Nothing reloads the page.
 */
export default function App() {
  const [filters, setFilters] = useState<Filters>(DEFAULT_FILTERS)
  const [page, setPage] = useState(1)
  const [myLocation, setMyLocation] = useState<LatLng | null>(null)
  const [myName, setMyName] = useState(loadMyName)
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
  const debouncedName = useDebounced(myName, 300)
  const context = useMemo(() => ({ location: myLocation, myName: debouncedName }), [myLocation, debouncedName])
  const listParams = useMemo(() => toQueryParams(debouncedFilters, context, page), [debouncedFilters, context, page])
  // The map gets the same filters, limited to the area in view.
  const pinParams = useMemo(
    () => (debouncedBounds ? { ...toQueryParams(debouncedFilters, context), ...boundsParams(debouncedBounds) } : null),
    [debouncedFilters, context, debouncedBounds],
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

  const updateFilters = useCallback((patch: Partial<Filters>) => {
    setFilters((f) => ({ ...f, ...patch }))
    setPage(1)
  }, [])

  /** Your contact name marks your ads; it's remembered in this browser. */
  const changeMyName = (name: string) => {
    setMyName(name)
    saveMyName(name)
    setPage(1)
  }

  /** Picking a distance needs the user's location; ask for it the first time. "Any" turns the filter off. */
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
      setFocus(here)
      updateFilters({ radiusKm: km })
    } catch {
      setLocationError(t.errLocationDenied)
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
        myName={myName}
        locating={locating}
        locationError={locationError}
        onChange={updateFilters}
        onMyName={changeMyName}
        onDistance={(km) => void changeDistance(km)}
        onClear={() => { setFilters(DEFAULT_FILTERS); setPage(1); setLocationError(null) }}
      />

      <main className="main">
        <AdsList
          result={result}
          loading={loading}
          error={error}
          filtered={filtered}
          selectedId={selectedId}
          myName={myName}
          onSelect={(ad) => openAd(ad.id, { lat: ad.latitude, lng: ad.longitude })}
          onEdit={(ad) => setPanel({ mode: 'edit', ad })}
          onDelete={(ad) => { openAd(ad.id, { lat: ad.latitude, lng: ad.longitude }); setAskDelete(true) }}
          onPage={setPage}
        />

        <AdsMap
          pins={pins}
          selectedId={selectedId}
          focus={focus}
          myLocation={filters.radiusKm !== null ? myLocation : null}
          radiusKm={filters.radiusKm}
          onSelect={(id) => openAd(id)}
          onBoundsChange={setBounds}
        />

        <AdPanel
          key={selectedId ?? panel.mode}
          state={panel}
          askDelete={askDelete}
          myName={myName}
          onClose={() => setPanel({ mode: 'closed' })}
          onEdit={(ad) => setPanel({ mode: 'edit', ad })}
          onSaved={(ad) => {
            changeMyName(ad.contactName) // the name you post under is how your ads are found
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
