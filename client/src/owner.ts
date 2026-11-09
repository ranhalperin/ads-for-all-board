const KEY = 'bulletin-board.owner-id'
let fallback: string | null = null

/**
 * The ID that marks ads as "mine". Created once per browser and kept in localStorage.
 * If storage is blocked (private mode), the ID lasts for this tab only.
 */
export function getOwnerId(): string {
  try {
    const existing = localStorage.getItem(KEY)
    if (existing && /^[A-Za-z0-9-]{8,64}$/.test(existing)) return existing
    const created = crypto.randomUUID()
    localStorage.setItem(KEY, created)
    return created
  } catch {
    fallback ??= crypto.randomUUID()
    return fallback
  }
}
