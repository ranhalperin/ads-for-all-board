export const CATEGORIES = ['ForSale', 'Wanted', 'RealEstate', 'Vehicles', 'Jobs', 'Services', 'Other'] as const
export type Category = (typeof CATEGORIES)[number]

export const SORTS = ['Newest', 'PriceAsc', 'PriceDesc', 'Distance'] as const
export type AdSort = (typeof SORTS)[number]

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

/** What the filters bar holds. Text inputs stay strings until the query is built. */
export interface Filters {
  search: string
  category: Category | ''
  minPrice: string
  maxPrice: string
  mineOnly: boolean
  nearMe: boolean
  radiusKm: number
  onlyMapArea: boolean
  sort: AdSort
}

export const DEFAULT_FILTERS: Filters = {
  search: '',
  category: '',
  minPrice: '',
  maxPrice: '',
  mineOnly: false,
  nearMe: false,
  radiusKm: 10,
  onlyMapArea: false,
  sort: 'Newest',
}

export type PanelState =
  | { mode: 'closed' }
  | { mode: 'view'; id: string }
  | { mode: 'new' }
  | { mode: 'edit'; ad: Ad }
