import type { Ad, AdRequest, Bounds, Filters, LatLng, PagedResult, PinsResult } from '../types'
import { http } from './http'

export const PAGE_SIZE = 20

export interface QueryContext {
  location: LatLng | null // for the distance filter
  myName: string // for "My ads"
}

/**
 * Turns the filters bar into API query parameters. Empty filters are left out; the distance is
 * sent only once the user's location is known, and "My ads" only once their name is.
 */
export function toQueryParams(filters: Filters, { location, myName }: QueryContext, page?: number): Record<string, string> {
  const params: Record<string, string> = {}
  const search = filters.search.trim()
  if (search) params.search = search
  if (filters.category) params.category = filters.category

  const min = parsePrice(filters.minPrice)
  const max = parsePrice(filters.maxPrice)
  if (min !== null) params.minPrice = String(min)
  if (max !== null) params.maxPrice = String(max)

  const name = myName.trim()
  if (filters.mineOnly && name) params.contactName = name

  if (filters.radiusKm !== null && location) {
    params.lat = location.lat.toFixed(6)
    params.lng = location.lng.toFixed(6)
    params.radiusKm = String(filters.radiusKm)
  }

  if (page !== undefined) {
    params.page = String(page)
    params.pageSize = String(PAGE_SIZE)
  }
  return params
}

export function boundsParams(b: Bounds): Record<string, string> {
  const clampLat = (v: number) => Math.max(-90, Math.min(90, v))
  const clampLng = (v: number) => Math.max(-180, Math.min(180, v))
  return {
    north: clampLat(b.north).toFixed(6),
    south: clampLat(b.south).toFixed(6),
    east: clampLng(b.east).toFixed(6),
    west: clampLng(b.west).toFixed(6),
  }
}

function parsePrice(value: string): number | null {
  if (value.trim() === '') return null
  const n = Number(value)
  return Number.isFinite(n) && n >= 0 ? n : null
}

export async function searchAds(params: Record<string, string>, signal: AbortSignal): Promise<PagedResult<Ad>> {
  return (await http.get<PagedResult<Ad>>('/ads', { params, signal })).data
}

export async function getPins(params: Record<string, string>, signal: AbortSignal): Promise<PinsResult> {
  return (await http.get<PinsResult>('/ads/pins', { params, signal })).data
}

export async function getAd(id: string, signal?: AbortSignal): Promise<Ad> {
  return (await http.get<Ad>(`/ads/${id}`, { signal })).data
}

export async function createAd(body: AdRequest): Promise<Ad> {
  return (await http.post<Ad>('/ads', body)).data
}

export async function updateAd(id: string, body: AdRequest): Promise<Ad> {
  return (await http.put<Ad>(`/ads/${id}`, body)).data
}

export async function deleteAd(id: string): Promise<void> {
  await http.delete(`/ads/${id}`)
}
