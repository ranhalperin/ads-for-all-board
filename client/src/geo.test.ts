import { describe, expect, it } from 'vitest'
import { locationErrorMessage } from './geo'
import { t } from './appText'

describe('locationErrorMessage', () => {
  it('names the reason the browser gave', () => {
    expect(locationErrorMessage({ code: 1 })).toBe(t.errLocationBlocked)
    expect(locationErrorMessage({ code: 2 })).toBe(t.errLocationUnavailable)
    expect(locationErrorMessage({ code: 3 })).toBe(t.errLocationTimeout)
  })

  it('treats anything else as unavailable', () => {
    expect(locationErrorMessage(new Error('Geolocation is not supported'))).toBe(t.errLocationUnavailable)
    expect(locationErrorMessage(null)).toBe(t.errLocationUnavailable)
  })
})
