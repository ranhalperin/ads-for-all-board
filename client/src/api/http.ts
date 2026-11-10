import axios from 'axios'
import { t } from '../appText'

/** One axios client for the API. */
export const http = axios.create({ baseURL: '/api', timeout: 30_000 })

interface ProblemDetails {
  title?: string
  status?: number
  errors?: Record<string, string[]>
}

/** A message safe to show: the server's ProblemDetails title, or a generic one. */
export function errorMessage(error: unknown): string {
  if (axios.isAxiosError(error)) {
    if (!error.response) return t.errNetwork
    const problem = error.response.data as ProblemDetails | undefined
    if (problem?.errors) return t.errFixFields
    if (problem?.title) return problem.title
  }
  return t.errGeneric
}

/** Field errors from a 400 validation response, keyed by camelCase field name. */
export function fieldErrors(error: unknown): Record<string, string> {
  if (!axios.isAxiosError(error)) return {}
  const errors = (error.response?.data as ProblemDetails | undefined)?.errors
  if (!errors) return {}
  return Object.fromEntries(
    Object.entries(errors).map(([key, messages]) => [
      key.replace(/^\$\./, '').replace(/^./, (c) => c.toLowerCase()),
      messages[0] ?? t.errGeneric,
    ]),
  )
}

export function isNotFound(error: unknown): boolean {
  return axios.isAxiosError(error) && error.response?.status === 404
}

export function isCanceled(error: unknown): boolean {
  return axios.isCancel(error)
}
