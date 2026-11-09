import { categoryLabel, sortLabel, t } from '../strings'
import { CATEGORIES, SORTS, type Filters } from '../types'

interface Props {
  filters: Filters
  locating: boolean
  locationError: string | null
  onChange: (patch: Partial<Filters>) => void
  onToggleNearMe: () => void
  onClear: () => void
}

const RADII = [1, 5, 10, 25, 50, 100]

export function FiltersBar({ filters, locating, locationError, onChange, onToggleNearMe, onClear }: Props) {
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

        <div className="near-me">
          <button
            type="button"
            className={filters.nearMe ? 'chip on' : 'chip'}
            aria-pressed={filters.nearMe}
            disabled={locating}
            onClick={onToggleNearMe}
          >
            ◎ {locating ? t.loading : t.nearMe}
          </button>
          {filters.nearMe && (
            <select
              aria-label={t.withinKm(filters.radiusKm)}
              value={filters.radiusKm}
              onChange={(e) => onChange({ radiusKm: Number(e.target.value) })}
            >
              {RADII.map((km) => (
                <option key={km} value={km}>
                  {t.withinKm(km)}
                </option>
              ))}
            </select>
          )}
        </div>

        <label className="check">
          <input type="checkbox" checked={filters.onlyMapArea} onChange={(e) => onChange({ onlyMapArea: e.target.checked })} />
          {t.onlyMapArea}
        </label>

        <label className="check">
          <input type="checkbox" checked={filters.mineOnly} onChange={(e) => onChange({ mineOnly: e.target.checked })} />
          {t.myAds}
        </label>

        <select
          aria-label={t.sortLabel}
          value={filters.sort}
          onChange={(e) => onChange({ sort: e.target.value as Filters['sort'] })}
        >
          {SORTS.filter((s) => s !== 'Distance' || filters.nearMe).map((s) => (
            <option key={s} value={s}>
              {sortLabel[s]}
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
