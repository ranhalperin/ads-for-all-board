import { useEffect, useState } from 'react'
import { deleteAd, getAd } from '../api/ads'
import { errorMessage, isCanceled, isNotFound } from '../api/http'
import { t } from '../strings'
import type { Ad, PanelState } from '../types'
import { AdDetails } from './AdDetails'
import { AdForm } from './AdForm'

interface Props {
  state: PanelState
  askDelete: boolean
  onClose: () => void
  onEdit: (ad: Ad) => void
  onSaved: (ad: Ad) => void
  onDeleted: (id: string) => void
  onAskDelete: (ask: boolean) => void
}

/** The side panel: shows an ad, or the form to create / edit one. */
export function AdPanel({ state, askDelete, onClose, onEdit, onSaved, onDeleted, onAskDelete }: Props) {
  const [loaded, setLoaded] = useState<{ id: string; ad?: Ad; error?: string } | null>(null)
  const [actionError, setActionError] = useState<string | null>(null)
  const [deleting, setDeleting] = useState(false)
  const viewId = state.mode === 'view' ? state.id : null

  // Always load the ad fresh: it may have changed since the list was fetched.
  useEffect(() => {
    if (!viewId) return
    const controller = new AbortController()
    getAd(viewId, controller.signal)
      .then((ad) => setLoaded({ id: viewId, ad }))
      .catch((err) => {
        if (!isCanceled(err)) setLoaded({ id: viewId, error: isNotFound(err) ? t.adNotFound : errorMessage(err) })
      })
    return () => controller.abort()
  }, [viewId])

  // On a phone the panel sits under the filters and the map, near the top of the page; the user may
  // have opened it from far down the list, so bring the top into view. (The panel remounts per ad.)
  const open = state.mode !== 'closed'
  useEffect(() => {
    if (open && window.matchMedia('(max-width: 900px)').matches) window.scrollTo({ top: 0 })
  }, [open])

  // What was loaded belongs to this panel only if it's for the ad being shown now.
  const current = loaded && loaded.id === viewId ? loaded : null
  const ad = current?.ad ?? null
  const error = actionError ?? current?.error ?? null

  if (state.mode === 'closed') return null

  const remove = async (target: Ad) => {
    setDeleting(true)
    setActionError(null)
    try {
      await deleteAd(target.id)
      onDeleted(target.id)
    } catch (err) {
      setActionError(errorMessage(err))
    } finally {
      setDeleting(false)
    }
  }

  return (
    <aside className="panel" aria-label="Ad">
      <button type="button" className="icon close" onClick={onClose} aria-label={t.close} title={t.close}>✕</button>

      {state.mode === 'new' && <AdForm key="new" ad={null} onSaved={onSaved} onCancel={onClose} />}
      {state.mode === 'edit' && <AdForm key={state.ad.id} ad={state.ad} onSaved={onSaved} onCancel={onClose} />}

      {state.mode === 'view' && (
        <>
          {!ad && !error && <p className="muted">{t.loading}</p>}
          {error && <p className="error-box" role="alert">{error}</p>}
          {ad && (
            <AdDetails
              ad={ad}
              confirmDelete={askDelete}
              deleting={deleting}
              onEdit={() => onEdit(ad)}
              onAskDelete={() => onAskDelete(true)}
              onCancelDelete={() => onAskDelete(false)}
              onDelete={() => void remove(ad)}
            />
          )}
        </>
      )}
    </aside>
  )
}
