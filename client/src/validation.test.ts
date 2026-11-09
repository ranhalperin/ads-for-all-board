import { describe, expect, it } from 'vitest'
import { shortLabel } from './api/geocoding'
import { EMPTY_FORM, toRequest, validateAd, type AdFormValues } from './validation'

const valid: AdFormValues = {
  ...EMPTY_FORM,
  title: 'City bike',
  description: 'Good condition',
  category: 'ForSale',
  price: '450',
  contactName: 'Dana',
  contactPhone: '050-1234567',
  location: { lat: 32.0853, lng: 34.7818 },
}

describe('validateAd', () => {
  it('accepts a complete ad', () => {
    expect(validateAd(valid)).toEqual({})
  })

  it('requires the mandatory fields and the location', () => {
    expect(Object.keys(validateAd(EMPTY_FORM)).sort()).toEqual(
      ['category', 'contactName', 'contactPhone', 'description', 'location', 'title'],
    )
  })

  it('checks lengths, price, phone and image link', () => {
    const errors = validateAd({ ...valid, title: 'ab', price: '-1', contactPhone: 'call me', imageUrl: 'ftp://x' })
    expect(Object.keys(errors).sort()).toEqual(['contactPhone', 'imageUrl', 'price', 'title'])
  })

  it('allows an empty price (free / negotiable)', () => {
    expect(validateAd({ ...valid, price: '' })).toEqual({})
  })
})

describe('toRequest', () => {
  it('trims text, turns an empty price into null and rounds coordinates', () => {
    const body = toRequest({ ...valid, title: '  City bike ', price: '', imageUrl: ' ', location: { lat: 32.08531234567, lng: 34.7 } })
    expect(body).toMatchObject({ title: 'City bike', price: null, imageUrl: null, latitude: 32.085312, address: null })
  })
})

describe('shortLabel', () => {
  it('keeps street, house number and city', () => {
    expect(shortLabel({
      display_name: 'long',
      address: { road: 'Dizengoff Street', house_number: '50', city: 'Tel Aviv-Yafo' },
    })).toBe('Dizengoff Street 50, Tel Aviv-Yafo')
  })

  it('falls back to the start of the display name', () => {
    expect(shortLabel({ display_name: 'Masada, Tamar Regional Council, Israel' })).toBe('Masada, Tamar Regional Council')
  })
})
