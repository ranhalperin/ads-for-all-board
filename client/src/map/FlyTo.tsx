import { useEffect } from 'react'
import { useMap } from 'react-leaflet'
import type { LatLng } from '../types'

/**
 * Moves the map to a point when it changes: flies there if it's off-screen, and zooms in to at
 * least `minZoom`. A point already in view at that zoom leaves the map where it is.
 */
export function FlyTo({ point, minZoom = 13 }: { point: LatLng | null; minZoom?: number }) {
  const map = useMap()
  useEffect(() => {
    if (!point) return
    // Wait a frame: selecting an ad also opens the side panel, which narrows the map first.
    const timer = setTimeout(() => {
      map.invalidateSize({ pan: false, animate: false })
      const inView = map.getBounds().pad(-0.15).contains([point.lat, point.lng])
      if (!inView || map.getZoom() < minZoom) {
        map.flyTo([point.lat, point.lng], Math.max(map.getZoom(), minZoom), { duration: 0.6 })
      }
    }, 50)
    return () => clearTimeout(timer)
  }, [map, point, minZoom])
  return null
}
