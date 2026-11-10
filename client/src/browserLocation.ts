import { t } from './appText'
import type { LatLng } from './types'

/** The browser's current position, or an error if the user refuses or it's unavailable. */
export function getCurrentLocation(): Promise<LatLng> {
  return new Promise((resolve, reject) => {
    if (!('geolocation' in navigator)) {
      reject(new Error('Geolocation is not supported'))
      return
    }
    navigator.geolocation.getCurrentPosition(
      (p) => resolve({ lat: p.coords.latitude, lng: p.coords.longitude }),
      reject,
      { enableHighAccuracy: false, timeout: 10_000, maximumAge: 60_000 },
    )
  })
}

/**
 * What to tell the user when getCurrentLocation fails, by the browser's error code:
 * 1 = the site is blocked, 2 = the device can't find itself, 3 = it took too long.
 */
export function locationErrorMessage(error: unknown): string {
  const code = (error as { code?: unknown } | null)?.code
  if (code === 1) return t.errLocationBlocked
  if (code === 3) return t.errLocationTimeout
  return t.errLocationUnavailable // code 2, or no geolocation in this browser
}

export const ISRAEL_CENTER: LatLng = { lat: 31.6, lng: 34.95 }
