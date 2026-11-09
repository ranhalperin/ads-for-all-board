import { useEffect, useRef } from 'react'
import { useMap } from 'react-leaflet'
import L from './leaflet'
import 'leaflet.markercluster'
import 'leaflet.markercluster/dist/MarkerCluster.css'
import 'leaflet.markercluster/dist/MarkerCluster.Default.css'
import type { AdPin } from '../types'

interface Props {
  pins: AdPin[]
  selectedId: string | null
  onSelect: (id: string) => void
}

const selectedIcon = L.divIcon({ className: 'pin-selected', iconSize: [22, 22], iconAnchor: [11, 11] })

/** Ad pins, merged into numbered bubbles where they're close together. */
export function ClusterLayer({ pins, selectedId, onSelect }: Props) {
  const map = useMap()
  const groupRef = useRef<L.MarkerClusterGroup | null>(null)
  const onSelectRef = useRef(onSelect)
  useEffect(() => {
    onSelectRef.current = onSelect
  }, [onSelect])

  useEffect(() => {
    const group = L.markerClusterGroup({ chunkedLoading: true, showCoverageOnHover: false, maxClusterRadius: 50 })
    groupRef.current = group
    map.addLayer(group)
    return () => {
      map.removeLayer(group)
      groupRef.current = null
    }
  }, [map])

  useEffect(() => {
    const group = groupRef.current
    if (!group) return
    group.clearLayers()
    const markers = pins.map((pin) => {
      const marker = L.marker([pin.latitude, pin.longitude], {
        title: pin.title,
        alt: pin.title,
        ...(pin.id === selectedId ? { icon: selectedIcon, zIndexOffset: 1000 } : {}),
      })
      marker.on('click', () => onSelectRef.current(pin.id))
      return marker
    })
    group.addLayers(markers)
  }, [pins, selectedId])

  return null
}
