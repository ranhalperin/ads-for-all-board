import { t } from './strings'

const shekel = new Intl.NumberFormat('en-IL', { style: 'currency', currency: 'ILS', maximumFractionDigits: 0 })
const date = new Intl.DateTimeFormat('en-GB', { day: 'numeric', month: 'short', year: 'numeric' })

export function formatPrice(price: number | null | undefined): string {
  if (price == null) return t.noPrice
  if (price === 0) return t.free
  return shekel.format(price)
}

export function formatDate(iso: string): string {
  return date.format(new Date(iso))
}
