import { t } from './strings'
import type { Ad, AdRequest, Category, LatLng } from './types'

/** The form's values. Text inputs stay strings; the location is set by the map. */
export interface AdFormValues {
  title: string
  description: string
  category: Category | ''
  price: string
  contactName: string
  contactPhone: string
  imageUrl: string
  location: LatLng | null
  address: string
}

export type FormErrors = Partial<Record<keyof AdFormValues, string>>

export const EMPTY_FORM: AdFormValues = {
  title: '',
  description: '',
  category: '',
  price: '',
  contactName: '',
  contactPhone: '',
  imageUrl: '',
  location: null,
  address: '',
}

export function formFromAd(ad: Ad): AdFormValues {
  return {
    title: ad.title,
    description: ad.description,
    category: ad.category,
    price: ad.price == null ? '' : String(ad.price),
    contactName: ad.contactName,
    contactPhone: ad.contactPhone,
    imageUrl: ad.imageUrl ?? '',
    location: { lat: ad.latitude, lng: ad.longitude },
    address: ad.address ?? '',
  }
}

/** Same rules as the API, so most mistakes are caught before sending. */
export function validateAd(v: AdFormValues): FormErrors {
  const errors: FormErrors = {}
  const title = v.title.trim()
  if (!title) errors.title = t.errRequired
  else if (title.length < 3 || title.length > 100) errors.title = t.errTitleLength

  const description = v.description.trim()
  if (!description) errors.description = t.errRequired
  else if (description.length > 2000) errors.description = t.errDescriptionLength

  if (!v.category) errors.category = t.errRequired

  if (v.price.trim() !== '') {
    const price = Number(v.price)
    if (!Number.isFinite(price) || price < 0 || price > 100_000_000) errors.price = t.errPrice
  }

  const name = v.contactName.trim()
  if (!name) errors.contactName = t.errRequired
  else if (name.length < 2 || name.length > 60) errors.contactName = t.errNameLength

  const phone = v.contactPhone.trim()
  if (!phone) errors.contactPhone = t.errRequired
  else if (!/^[0-9+\-\s()]{7,20}$/.test(phone)) errors.contactPhone = t.errPhone

  const image = v.imageUrl.trim()
  if (image && !/^https?:\/\/\S+$/i.test(image)) errors.imageUrl = t.errImageUrl

  if (!v.location) errors.location = t.errLocation
  return errors
}

export function toRequest(v: AdFormValues): AdRequest {
  if (!v.location || !v.category) throw new Error('Validate the form before converting it')
  return {
    title: v.title.trim(),
    description: v.description.trim(),
    category: v.category,
    price: v.price.trim() === '' ? null : Number(v.price),
    contactName: v.contactName.trim(),
    contactPhone: v.contactPhone.trim(),
    imageUrl: v.imageUrl.trim() || null,
    latitude: Number(v.location.lat.toFixed(6)),
    longitude: Number(v.location.lng.toFixed(6)),
    address: v.address.trim() || null,
  }
}
