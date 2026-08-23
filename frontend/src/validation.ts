import { getDomain } from 'tldts'

export type FormErrors = Partial<Record<'originalUrl' | 'customAlias' | 'ios' | 'android', string>>

const aliasPattern = /^[A-Za-z0-9_-]+$/
const reservedAliases = new Set(['api', 'swagger', 'health', 'stats'])

export function parseHttpUrl(value: string): URL | null {
  try {
    const url = new URL(value.trim())
    return url.protocol === 'http:' || url.protocol === 'https:' ? url : null
  } catch {
    return null
  }
}

export function sameRegistrableDomain(original: URL, candidate: URL): boolean {
  // The Public Suffix List handles multi-part suffixes such as co.th safely.
  const originalDomain = getDomain(original.hostname)
  const candidateDomain = getDomain(candidate.hostname)
  if (originalDomain && candidateDomain) {
    return originalDomain.toLowerCase() === candidateDomain.toLowerCase()
  }

  return original.hostname.toLowerCase() === candidate.hostname.toLowerCase()
}

export function validateForm(
  originalValue: string,
  aliasValue: string,
  platformEnabled: boolean,
  iosValue: string,
  androidValue: string,
): FormErrors {
  const errors: FormErrors = {}
  const original = parseHttpUrl(originalValue)
  if (!original) {
    errors.originalUrl = 'Enter a complete http or https URL.'
  }

  const alias = aliasValue.trim()
  if (alias && (!aliasPattern.test(alias) || reservedAliases.has(alias.toLowerCase()))) {
    errors.customAlias = 'Use letters, numbers, hyphens, or underscores. Reserved aliases are unavailable.'
  }

  if (!platformEnabled || !original) {
    return errors
  }

  for (const [field, value] of [['ios', iosValue], ['android', androidValue]] as const) {
    if (!value.trim()) {
      continue
    }
    const platformUrl = parseHttpUrl(value)
    if (!platformUrl) {
      errors[field] = 'Enter a complete http or https URL.'
    } else if (!sameRegistrableDomain(original, platformUrl)) {
      errors[field] = 'This destination must use the same registrable domain as the default URL.'
    }
  }

  return errors
}
