import { describe, expect, it } from 'vitest'
import { isMine } from './myName'

describe('isMine', () => {
  it('matches the whole contact name, ignoring case and outer spaces', () => {
    expect(isMine({ contactName: 'Dana' }, ' dana ')).toBe(true)
    expect(isMine({ contactName: 'Dana Levi' }, 'Dana')).toBe(false)
  })

  it('is never true without a name', () => {
    expect(isMine({ contactName: 'Dana' }, '  ')).toBe(false)
  })
})
