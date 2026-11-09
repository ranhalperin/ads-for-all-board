import type { Ad } from './types'

const KEY = 'bulletin-board.my-name'

/**
 * There are no accounts: the contact name you post under is what marks an ad as yours.
 * The browser remembers it from your last saved ad.
 */
export function loadMyName(): string {
  try {
    return localStorage.getItem(KEY) ?? ''
  } catch {
    return '' // storage blocked (private mode): the name lasts for this tab only
  }
}

export function saveMyName(name: string): void {
  try {
    localStorage.setItem(KEY, name.trim())
  } catch {
    // storage blocked: keep it in memory only
  }
}

/** The whole name, ignoring case and outer spaces. */
export function isMine(ad: Pick<Ad, 'contactName'>, myName: string): boolean {
  const me = myName.trim().toLowerCase()
  return me !== '' && ad.contactName.trim().toLowerCase() === me
}
