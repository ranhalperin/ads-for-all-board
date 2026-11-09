import { describe, expect, it } from 'vitest'
import { DEFAULT_FILTERS } from '../types'
import { boundsParams, toQueryParams } from './ads'

const noCtx = { location: null, bounds: null }
const here = { lat: 32.0853, lng: 34.7818 }

describe('toQueryParams', () => {
  it('sends only the sort when no filter is set', () => {
    expect(toQueryParams(DEFAULT_FILTERS, noCtx)).toEqual({ sort: 'Newest' })
  })

  it('adds paging when a page is given', () => {
    expect(toQueryParams(DEFAULT_FILTERS, noCtx, 3)).toMatchObject({ page: '3', pageSize: '20' })
  })

  it('trims the search and drops empty or invalid prices', () => {
    const params = toQueryParams({ ...DEFAULT_FILTERS, search: '  bike ', minPrice: '-5', maxPrice: '300' }, noCtx)
    expect(params).toEqual({ search: 'bike', maxPrice: '300', sort: 'Newest' })
  })

  it('sends the location and radius only when near me is on and the location is known', () => {
    const on = { ...DEFAULT_FILTERS, nearMe: true, radiusKm: 5, sort: 'Distance' as const }
    expect(toQueryParams(on, { location: here, bounds: null })).toMatchObject({
      lat: '32.085300', lng: '34.781800', radiusKm: '5', sort: 'Distance',
    })
    // No location yet: no radius, and distance sort falls back to newest.
    expect(toQueryParams(on, noCtx)).toEqual({ sort: 'Newest' })
  })

  it('sends the map area only when "only map area" is checked', () => {
    const bounds = { north: 32.2, south: 31.9, east: 35, west: 34.6 }
    expect(toQueryParams(DEFAULT_FILTERS, { location: null, bounds })).not.toHaveProperty('north')
    expect(toQueryParams({ ...DEFAULT_FILTERS, onlyMapArea: true }, { location: null, bounds })).toMatchObject({
      north: '32.200000', west: '34.600000',
    })
  })

  it('sends mine only and the category', () => {
    expect(toQueryParams({ ...DEFAULT_FILTERS, mineOnly: true, category: 'Jobs' }, noCtx)).toMatchObject({
      mineOnly: 'true', category: 'Jobs',
    })
  })
})

describe('boundsParams', () => {
  it('clamps a zoomed-out world view to valid coordinates', () => {
    expect(boundsParams({ north: 95, south: -95, east: 400, west: -400 })).toEqual({
      north: '90.000000', south: '-90.000000', east: '180.000000', west: '-180.000000',
    })
  })
})
