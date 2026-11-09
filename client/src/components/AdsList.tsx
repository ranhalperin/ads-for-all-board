import { formatPrice } from '../format'
import { categoryLabel, t } from '../strings'
import type { Ad, PagedResult } from '../types'

interface Props {
  result: PagedResult<Ad> | null
  loading: boolean
  error: string | null
  filtered: boolean
  selectedId: string | null
  onSelect: (ad: Ad) => void
  onEdit: (ad: Ad) => void
  onDelete: (ad: Ad) => void
  onPage: (page: number) => void
}

export function AdsList({ result, loading, error, filtered, selectedId, onSelect, onEdit, onDelete, onPage }: Props) {
  const pages = result ? Math.max(1, Math.ceil(result.total / result.pageSize)) : 1

  return (
    <section className="list" aria-label="Ads" aria-busy={loading}>
      <div className="list-head">
        <span>{result ? t.results(result.total) : t.loading}</span>
        {loading && result && <span className="muted">{t.loading}</span>}
      </div>

      {error && <p className="error-box" role="alert">{error}</p>}

      {result && result.items.length === 0 && !error && (
        <div className="empty">
          <strong>{filtered ? t.noResultsTitle : t.emptyBoardTitle}</strong>
          <p>{filtered ? t.noResultsBody : t.emptyBoardBody}</p>
        </div>
      )}

      <ul className="rows">
        {result?.items.map((ad) => (
          <li key={ad.id}>
            <div
              className={ad.id === selectedId ? 'row selected' : 'row'}
              role="button"
              tabIndex={0}
              onClick={() => onSelect(ad)}
              onKeyDown={(e) => (e.key === 'Enter' || e.key === ' ') && (e.preventDefault(), onSelect(ad))}
            >
              <Thumb ad={ad} />
              <div className="row-body">
                <div className="row-title">
                  <span className="title-text">{ad.title}</span>
                  {ad.isMine && <span className="badge mine">{t.yours}</span>}
                </div>
                <div className="row-meta">
                  <span className="price">{formatPrice(ad.price)}</span>
                  <span className="badge">{categoryLabel[ad.category]}</span>
                  {ad.distanceKm != null && <span>{t.kmAway(ad.distanceKm)}</span>}
                </div>
                {ad.address && <div className="row-address">{ad.address}</div>}
              </div>
              {ad.isMine && (
                <div className="row-actions">
                  <button type="button" className="icon" title={t.edit} aria-label={`${t.edit} ${ad.title}`}
                    onClick={(e) => { e.stopPropagation(); onEdit(ad) }}>✎</button>
                  <button type="button" className="icon danger" title={t.delete} aria-label={`${t.delete} ${ad.title}`}
                    onClick={(e) => { e.stopPropagation(); onDelete(ad) }}>🗑</button>
                </div>
              )}
            </div>
          </li>
        ))}
      </ul>

      {result && pages > 1 && (
        <nav className="pager" aria-label="Pages">
          <button type="button" disabled={result.page <= 1} onClick={() => onPage(result.page - 1)}>{t.previous}</button>
          <span>{t.page(result.page, pages)}</span>
          <button type="button" disabled={result.page >= pages} onClick={() => onPage(result.page + 1)}>{t.next}</button>
        </nav>
      )}
    </section>
  )
}

function Thumb({ ad }: { ad: Ad }) {
  if (ad.imageUrl) {
    return <img className="thumb" src={ad.imageUrl} alt="" loading="lazy" onError={(e) => (e.currentTarget.style.visibility = 'hidden')} />
  }
  return <div className="thumb placeholder" aria-hidden="true">{ad.title.charAt(0).toUpperCase()}</div>
}
