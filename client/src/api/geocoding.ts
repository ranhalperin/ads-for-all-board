import type { LatLng } from '../types'

/**
 * Address search and reverse lookup through OpenStreetMap's Nominatim (no key needed).
 * Its usage policy allows about one request per second, so callers search on Enter / button
 * and look up an address only after the pin has settled.
 */
const BASE = 'https://nominatim.openstreetmap.org'

export interface Place extends LatLng {
  label: string
}

interface NominatimResult {
  lat: string
  lon: string
  display_name: string
  address?: Record<string, string>
}

export async function searchAddress(query: string, signal?: AbortSignal): Promise<Place[]> {
  const url = `${BASE}/search?format=jsonv2&limit=5&addressdetails=1&accept-language=en&q=${encodeURIComponent(query)}`
  const response = await fetch(url, { signal })
  if (!response.ok) throw new Error(`Address search failed (${response.status})`)
  const results = (await response.json()) as NominatimResult[]
  // One square can come back as several map objects with the same short name: keep the first of each.
  const seen = new Set<string>()
  return results
    .map((r) => ({ lat: Number(r.lat), lng: Number(r.lon), label: shortLabel(r) }))
    .filter((p) => !seen.has(p.label) && seen.add(p.label))
}

export async function reverseGeocode(point: LatLng, signal?: AbortSignal): Promise<string | null> {
  const url = `${BASE}/reverse?format=jsonv2&zoom=17&addressdetails=1&accept-language=en&lat=${point.lat}&lon=${point.lng}`
  const response = await fetch(url, { signal })
  if (!response.ok) return null
  const result = (await response.json()) as NominatimResult & { error?: string }
  return result.error ? null : shortLabel(result)
}

/** "Dizengoff Street 50, Tel Aviv-Yafo" rather than the full administrative chain. */
export function shortLabel(r: Pick<NominatimResult, 'display_name' | 'address'>): string {
  const a = r.address ?? {}
  const street = [a.road ?? a.pedestrian ?? a.square ?? a.neighbourhood, a.house_number].filter(Boolean).join(' ')
  const place = a.city ?? a.town ?? a.village ?? a.municipality ?? a.county
  const short = [street, place].filter(Boolean).join(', ')
  return short || r.display_name.split(',').slice(0, 2).join(',').trim()
}
