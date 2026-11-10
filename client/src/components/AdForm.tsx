import { useState, type ReactNode } from 'react'
import { createAd, updateAd } from '../api/adsApi'
import { errorMessage, fieldErrors } from '../api/apiClient'
import { categoryLabel, t } from '../appText'
import { CATEGORIES, type Ad, type Category } from '../types'
import { EMPTY_FORM, formFromAd, toRequest, validateAd, type AdFormValues, type FormErrors } from '../adForm'
import { AdLocationPicker } from './AdLocationPicker'

interface Props {
  ad: Ad | null // null = new ad
  onSaved: (ad: Ad) => void
  onCancel: () => void
}

export function AdForm({ ad, onSaved, onCancel }: Props) {
  const [values, setValues] = useState<AdFormValues>(() => (ad ? formFromAd(ad) : EMPTY_FORM))
  const [errors, setErrors] = useState<FormErrors>({})
  const [saving, setSaving] = useState(false)
  const [formError, setFormError] = useState<string | null>(null)

  const set = <K extends keyof AdFormValues>(key: K, value: AdFormValues[K]) => {
    setValues((v) => ({ ...v, [key]: value }))
    setErrors((e) => ({ ...e, [key]: undefined }))
  }

  const submit = async (e: React.FormEvent) => {
    e.preventDefault()
    const found = validateAd(values)
    setErrors(found)
    if (Object.keys(found).length > 0) {
      setFormError(t.errFixFields)
      return
    }
    setSaving(true)
    setFormError(null)
    try {
      const body = toRequest(values)
      onSaved(ad ? await updateAd(ad.id, body) : await createAd(body))
    } catch (err) {
      const server = fieldErrors(err)
      if (server.latitude || server.longitude) server.location = server.latitude ?? server.longitude
      setErrors(server as FormErrors)
      setFormError(errorMessage(err))
    } finally {
      setSaving(false)
    }
  }

  return (
    <form className="ad-form" onSubmit={submit} noValidate>
      <h2>{ad ? t.editAdTitle : t.newAdTitle}</h2>

      <FormField label={t.fieldTitle} error={errors.title} required>
        <input value={values.title} maxLength={100} placeholder={t.placeholderTitle}
          onChange={(e) => set('title', e.target.value)} />
      </FormField>

      <FormField label={t.fieldDescription} error={errors.description} required>
        <textarea rows={4} value={values.description} maxLength={2000} placeholder={t.placeholderDescription}
          onChange={(e) => set('description', e.target.value)} />
      </FormField>

      <div className="two">
        <FormField label={t.fieldCategory} error={errors.category} required>
          <select value={values.category} onChange={(e) => set('category', e.target.value as Category | '')}>
            <option value="">{t.chooseCategory}</option>
            {CATEGORIES.map((c) => <option key={c} value={c}>{categoryLabel[c]}</option>)}
          </select>
        </FormField>
        <FormField label={t.fieldPrice} error={errors.price} hint={t.fieldPriceHint}>
          <input type="number" min={0} inputMode="numeric" value={values.price}
            onChange={(e) => set('price', e.target.value)} />
        </FormField>
      </div>

      <div className="two">
        <FormField label={t.fieldContactName} error={errors.contactName} required>
          <input value={values.contactName} maxLength={60} placeholder={t.placeholderName} autoComplete="name"
            onChange={(e) => set('contactName', e.target.value)} />
        </FormField>
        <FormField label={t.fieldContactPhone} error={errors.contactPhone} required>
          <input type="tel" value={values.contactPhone} maxLength={20} placeholder={t.placeholderPhone} autoComplete="tel"
            onChange={(e) => set('contactPhone', e.target.value)} />
        </FormField>
      </div>

      <FormField label={t.fieldImageUrl} error={errors.imageUrl}>
        <input type="url" value={values.imageUrl} maxLength={500} placeholder={t.placeholderImage}
          onChange={(e) => set('imageUrl', e.target.value)} />
      </FormField>

      <div className="field">
        <span className="label">{t.fieldLocation} <span className="req">*</span></span>
        <AdLocationPicker
          value={values.location}
          address={values.address}
          error={errors.location}
          onChange={(location, address) => {
            setValues((v) => ({ ...v, location, address: address ?? '' }))
            setErrors((e) => ({ ...e, location: undefined }))
          }}
          onAddress={(address) => setValues((v) => ({ ...v, address }))}
        />
      </div>

      {formError && <p className="error-box" role="alert">{formError}</p>}

      <div className="form-actions">
        <button type="button" onClick={onCancel} disabled={saving}>{t.cancel}</button>
        <button type="submit" className="primary" disabled={saving}>{saving ? t.saving : t.save}</button>
      </div>
    </form>
  )
}

function FormField({ label, error, hint, required, children }: {
  label: string
  error?: string
  hint?: string
  required?: boolean
  children: ReactNode
}) {
  return (
    <label className={error ? 'field invalid' : 'field'}>
      <span className="label">{label}{required && <span className="req"> *</span>}</span>
      {children}
      {error ? <span className="field-error">{error}</span> : hint && <span className="hint">{hint}</span>}
    </label>
  )
}
