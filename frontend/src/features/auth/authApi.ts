export async function getCsrfToken(): Promise<string> {
  const response = await fetch('/api/v1/auth/csrf', { credentials: 'include', cache: 'no-store' })
  if (!response.ok) throw new Error('No se pudo preparar una solicitud segura. Recarga la página.')
  const body = await response.json() as { token: string }
  return body.token
}

export async function postJson<T = void>(url: string, body?: unknown): Promise<T> {
  const token = await getCsrfToken()
  const response = await fetch(url, {
    method: 'POST',
    credentials: 'include',
    cache: 'no-store',
    headers: { 'Content-Type': 'application/json', 'X-CSRF-TOKEN': token },
    body: body === undefined ? '{}' : JSON.stringify(body),
  })
  const responseBody = response.status === 204 ? undefined : await response.json().catch(() => undefined)
  if (!response.ok) throw new Error(responseBody?.message ?? 'No se pudo completar la solicitud.')
  return responseBody as T
}

export async function putJson<T>(url: string, body: unknown): Promise<T> {
  const token = await getCsrfToken()
  const response = await fetch(url, {
    method: 'PUT', credentials: 'include', cache: 'no-store',
    headers: { 'Content-Type': 'application/json', 'X-CSRF-TOKEN': token },
    body: JSON.stringify(body),
  })
  const responseBody = await response.json().catch(() => undefined)
  if (!response.ok) throw new Error(responseBody?.message ?? 'No se pudo completar la solicitud.')
  return responseBody as T
}

export async function postForm<T>(url: string, body: FormData): Promise<T> {
  const token = await getCsrfToken()
  const response = await fetch(url, {
    method: 'POST', credentials: 'include', cache: 'no-store',
    headers: { 'X-CSRF-TOKEN': token }, body,
  })
  const responseBody = await response.json().catch(() => undefined)
  if (!response.ok) throw new Error(responseBody?.message ?? 'No se pudo completar la solicitud.')
  return responseBody as T
}

export async function deleteRequest(url: string): Promise<void> {
  const token = await getCsrfToken()
  const response = await fetch(url, {
    method: 'DELETE', credentials: 'include', cache: 'no-store',
    headers: { 'X-CSRF-TOKEN': token },
  })
  if (!response.ok) throw new Error('No se pudo completar la solicitud.')
}
