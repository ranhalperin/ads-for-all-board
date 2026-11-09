export const CATEGORIES = ['ForSale', 'Wanted', 'RealEstate', 'Vehicles', 'Jobs', 'Services', 'Other'] as const
export type Category = (typeof CATEGORIES)[number]

export interface LatLng {
  lat: number
  lng: number
}

export interface Bounds {
  north: number
  south: number
  east: number
  west: number
}

/** An ad as the API returns it. The owner ID is never sent; `isMine` says whether it's yours. */
export interface Ad {
  id: string
  title: string
  description: string
  category: Category
  price?: number | null
  contactName: string
  contactPhone: string
  imageUrl?: string | null
  latitude: number
  longitude: number
  address?: string | null
  createdAt: string
  updatedAt?: string | null
  isMine: boolean
  distanceKm?: number | null
}

export interface PagedResult<T> {
  items: T[]
  total: number
  page: number
  pageSize: number
}

export interface AdPin {
  id: string
  title: string
  category: Category
  latitude: number
  longitude: number
}

export interface PinsResult {
  pins: AdPin[]
  total: number
  truncated: boolean
}

/** Body of POST / PUT. */
export interface AdRequest {
  title: string
  description: string
  category: Category
  price: number | null
  contactName: string
  contactPhone: string
  imageUrl: string | null
  latitude: number
  longitude: number
  address: string | null
}

/** What the filters bar holds. Text inputs stay strings until the query is built. Results are always newest first. */
export interface Filters {
  search: string
  mineOnly: boolean
  category: Category | ''
  minPrice: string
  maxPrice: string
  /** Distance from the user's location; null = any distance. */
  radiusKm: number | null
}

export const DEFAULT_FILTERS: Filters = {
  search: '',
  mineOnly: false,
  category: '',
  minPrice: '',
  maxPrice: '',
  radiusKm: null,
}

export const DISTANCES_KM = [1, 5, 10, 25, 50, 100] as const

export type PanelState =
  | { mode: 'closed' }
  | { mode: 'view'; id: string }
  | { mode: 'new' }
  | { mode: 'edit'; ad: Ad }
