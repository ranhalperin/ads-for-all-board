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

export const ISRAEL_CENTER: LatLng = { lat: 31.6, lng: 34.95 }

export const googleMapsUrl = (p: LatLng) => `https://www.google.com/maps/search/?api=1&query=${p.lat},${p.lng}`
export const wazeUrl = (p: LatLng) => `https://waze.com/ul?ll=${p.lat},${p.lng}&navigate=yes`
