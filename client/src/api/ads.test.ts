import { describe, expect, it } from 'vitest'
import { DEFAULT_FILTERS } from '../types'
import { boundsParams, toQueryParams } from './ads'

const here = { lat: 32.0853, lng: 34.7818 }

describe('toQueryParams', () => {
  it('sends nothing when no filter is set', () => {
    expect(toQueryParams(DEFAULT_FILTERS, null)).toEqual({})
  })

  it('adds paging when a page is given', () => {
    expect(toQueryParams(DEFAULT_FILTERS, null, 3)).toEqual({ page: '3', pageSize: '20' })
  })

  it('trims the search and drops empty or invalid prices', () => {
    const params = toQueryParams({ ...DEFAULT_FILTERS, search: '  bike ', minPrice: '-5', maxPrice: '300' }, null)
    expect(params).toEqual({ search: 'bike', maxPrice: '300' })
  })

  it('sends my ads and the category', () => {
    expect(toQueryParams({ ...DEFAULT_FILTERS, mineOnly: true, category: 'Jobs' }, null)).toEqual({
      mineOnly: 'true', category: 'Jobs',
    })
  })

  it('sends the distance with the location once the location is known', () => {
    const within5 = { ...DEFAULT_FILTERS, radiusKm: 5 }
    expect(toQueryParams(within5, here)).toEqual({ lat: '32.085300', lng: '34.781800', radiusKm: '5' })
    expect(toQueryParams(within5, null)).toEqual({})
  })

  it('sends no distance for "any distance", even when the location is known', () => {
    expect(toQueryParams(DEFAULT_FILTERS, here)).toEqual({})
  })
})

describe('boundsParams', () => {
  it('clamps a zoomed-out world view to valid coordinates', () => {
    expect(boundsParams({ north: 95, south: -95, east: 400, west: -400 })).toEqual({
      north: '90.000000', south: '-90.000000', east: '180.000000', west: '-180.000000',
    })
  })
})
