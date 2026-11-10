import { useState } from 'react'
import { MapContainer, Marker, TileLayer } from 'react-leaflet'
import { formatDate, formatPrice } from '../format'
import { AutoResize } from '../map/AutoResize'
import { TILE_ATTRIBUTION, TILE_URL } from '../map/leaflet'
import { categoryLabel, t } from '../strings'
import type { Ad } from '../types'

interface Props {
  ad: Ad
  confirmDelete: boolean
  deleting: boolean
  onEdit: () => void
  onAskDelete: () => void
  onCancelDelete: () => void
  onDelete: () => void
}

export function AdDetails({ ad, confirmDelete, deleting, onEdit, onAskDelete, onCancelDelete, onDelete }: Props) {
  const [imageOk, setImageOk] = useState(true)
  const point = { lat: ad.latitude, lng: ad.longitude }

  return (
    <article className="details">
      {ad.imageUrl && imageOk && <img className="details-image" src={ad.imageUrl} alt="" onError={() => setImageOk(false)} />}

      <div className="details-head">
        <span className="badge">{categoryLabel[ad.category]}</span>
      </div>
      <h2>{ad.title}</h2>
      <p className="details-price">{formatPrice(ad.price)}</p>
      <p className="details-text">{ad.description}</p>

      <dl className="details-contact">
        <dt>{t.contact}</dt>
        <dd>{ad.contactName} · <a href={`tel:${ad.contactPhone.replace(/[^\d+]/g, '')}`}>{ad.contactPhone}</a></dd>
      </dl>

      <MapContainer key={ad.id} center={[point.lat, point.lng]} zoom={14} className="details-map"
        dragging={false} scrollWheelZoom={false} doubleClickZoom={false} zoomControl={false}>
        <TileLayer url={TILE_URL} attribution={TILE_ATTRIBUTION} />
        <AutoResize />
        <Marker position={[point.lat, point.lng]} />
      </MapContainer>
      {ad.address && <p className="muted">📍 {ad.address}{ad.distanceKm != null && ` · ${t.kmAway(ad.distanceKm)}`}</p>}

      <p className="muted small">
        {t.posted(formatDate(ad.createdAt))}
        {ad.updatedAt && `, ${t.edited(formatDate(ad.updatedAt))}`}
      </p>

      {!confirmDelete && (
        <div className="form-actions">
          <button type="button" className="danger" onClick={onAskDelete}>{t.delete}</button>
          <button type="button" className="primary" onClick={onEdit}>{t.edit}</button>
        </div>
      )}
      {confirmDelete && (
        <div className="confirm" role="alertdialog" aria-label={t.deleteConfirm}>
          <p>{t.deleteConfirm}</p>
          <div className="form-actions">
            <button type="button" onClick={onCancelDelete} disabled={deleting}>{t.cancel}</button>
            <button type="button" className="danger solid" onClick={onDelete} disabled={deleting}>{t.deleteYes}</button>
          </div>
        </div>
      )}
    </article>
  )
}
