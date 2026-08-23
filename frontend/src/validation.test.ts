import { describe, expect, it } from 'vitest'
import { parseHttpUrl, sameRegistrableDomain, validateForm } from './validation'

describe('link validation', () => {
  it('accepts sibling subdomains under a multi-part registrable domain', () => {
    const original = parseHttpUrl('https://www.gulf.co.th')!
    const platform = parseHttpUrl('https://download.gulf.co.th/app.apk')!

    expect(sameRegistrableDomain(original, platform)).toBe(true)
  })

  it('rejects a platform destination from another registrable domain', () => {
    const errors = validateForm(
      'https://www.gulf.co.th',
      '',
      true,
      'https://example.com/app.ipa',
      '',
    )

    expect(errors.ios).toContain('same registrable domain')
  })

  it('allows platform routing with both destinations empty', () => {
    expect(validateForm('https://example.com', '', true, '', '')).toEqual({})
  })
})
