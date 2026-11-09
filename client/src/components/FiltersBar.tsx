import { categoryLabel, t } from '../strings'
import { CATEGORIES, DISTANCES_KM, type Filters } from '../types'

interface Props {
  filters: Filters
  myName: string
  locating: boolean
  locationError: string | null
  onChange: (patch: Partial<Filters>) => void
  onMyName: (name: string) => void
  onDistance: (km: number | null) => void
  onClear: () => void
}

/** Search by title, then: my ads (by your contact name), category, price from–to, distance. */
export function FiltersBar({ filters, myName, locating, locationError, onChange, onMyName, onDistance, onClear }: Props) {
  return (
    <section className="filters" aria-label="Filters">
      <input
        type="search"
        className="search"
        placeholder={t.searchPlaceholder}
        aria-label={t.searchPlaceholder}
        value={filters.search}
        maxLength={100}
        onChange={(e) => onChange({ search: e.target.value })}
      />

      <div className="filter-row">
        <label className="check">
          <input type="checkbox" checked={filters.mineOnly} onChange={(e) => onChange({ mineOnly: e.target.checked })} />
          {t.myAds}
        </label>
        {filters.mineOnly && (
          <input
            className="my-name"
            placeholder={t.yourName}
            aria-label={t.yourName}
            value={myName}
            maxLength={60}
            autoComplete="name"
            autoFocus={!myName}
            onChange={(e) => onMyName(e.target.value)}
          />
        )}

        <select
          aria-label={t.allCategories}
          value={filters.category}
          onChange={(e) => onChange({ category: e.target.value as Filters['category'] })}
        >
          <option value="">{t.allCategories}</option>
          {CATEGORIES.map((c) => (
            <option key={c} value={c}>
              {categoryLabel[c]}
            </option>
          ))}
        </select>

        <div className="price-range">
          <input
            type="number"
            min={0}
            inputMode="numeric"
            placeholder={t.minPrice}
            aria-label={t.minPrice}
            value={filters.minPrice}
            onChange={(e) => onChange({ minPrice: e.target.value })}
          />
          <span aria-hidden="true">–</span>
          <input
            type="number"
            min={0}
            inputMode="numeric"
            placeholder={t.maxPrice}
            aria-label={t.maxPrice}
            value={filters.maxPrice}
            onChange={(e) => onChange({ maxPrice: e.target.value })}
          />
        </div>

        <select
          aria-label={t.distance}
          className={filters.radiusKm !== null ? 'on' : undefined}
          value={filters.radiusKm ?? ''}
          disabled={locating}
          onChange={(e) => onDistance(e.target.value === '' ? null : Number(e.target.value))}
        >
          <option value="">{locating ? t.locating : t.anyDistance}</option>
          {DISTANCES_KM.map((km) => (
            <option key={km} value={km}>
              {t.withinKm(km)}
            </option>
          ))}
        </select>

        <button type="button" className="link" onClick={onClear}>
          {t.clearFilters}
        </button>
      </div>
      {locationError && <p className="field-error" role="alert">{locationError}</p>}
    </section>
  )
}
