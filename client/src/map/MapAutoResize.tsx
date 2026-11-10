import { useEffect } from 'react'
import { useMap } from 'react-leaflet'

/**
 * Leaflet measures its box once. When the box changes size (the side panel opens, the window is
 * resized), it must re-measure, or parts of the map stay grey.
 */
export function MapAutoResize() {
  const map = useMap()
  useEffect(() => {
    const container = map.getContainer()
    // Keep the same centre when the box changes, so what the user was looking at stays in view.
    const observer = new ResizeObserver(() => map.invalidateSize({ pan: true, animate: false }))
    observer.observe(container)
    return () => observer.disconnect()
  }, [map])
  return null
}
