import { afterEach, describe, expect, it, vi } from 'vitest'
import { getCsrfToken, postJson } from './authApi'

describe('authApi', () => {
  afterEach(() => vi.unstubAllGlobals())

  it('obtiene el token antiforgery sin cachear la respuesta', async () => {
    const fetchMock = vi.fn().mockResolvedValue(new Response(JSON.stringify({ token: 'csrf-token' }), { status: 200 }))
    vi.stubGlobal('fetch', fetchMock)

    await expect(getCsrfToken()).resolves.toBe('csrf-token')
    expect(fetchMock).toHaveBeenCalledWith('/api/v1/auth/csrf', { credentials: 'include', cache: 'no-store' })
  })

  it('notifica cuando no se puede preparar una solicitud segura', async () => {
    vi.stubGlobal('fetch', vi.fn().mockResolvedValue(new Response(null, { status: 500 })))

    await expect(getCsrfToken()).rejects.toThrow('No se pudo preparar una solicitud segura. Recarga la página.')
  })

  it('envía JSON con cookie y token y devuelve la respuesta', async () => {
    const fetchMock = vi.fn()
      .mockResolvedValueOnce(new Response(JSON.stringify({ token: 'csrf-token' }), { status: 200 }))
      .mockResolvedValueOnce(new Response(JSON.stringify({ id: 'user-id' }), { status: 201 }))
    vi.stubGlobal('fetch', fetchMock)

    await expect(postJson('/api/v1/auth/login', { email: 'club@example.es' })).resolves.toEqual({ id: 'user-id' })
    expect(fetchMock).toHaveBeenNthCalledWith(2, '/api/v1/auth/login', expect.objectContaining({
      method: 'POST', credentials: 'include', cache: 'no-store',
      headers: { 'Content-Type': 'application/json', 'X-CSRF-TOKEN': 'csrf-token' },
      body: JSON.stringify({ email: 'club@example.es' }),
    }))
  })

  it('maneja respuestas sin cuerpo y errores del servidor', async () => {
    const fetchMock = vi.fn()
      .mockResolvedValueOnce(new Response(JSON.stringify({ token: 'csrf-token' }), { status: 200 }))
      .mockResolvedValueOnce(new Response(null, { status: 204 }))
    vi.stubGlobal('fetch', fetchMock)
    await expect(postJson('/logout')).resolves.toBeUndefined()

    const failingFetch = vi.fn()
      .mockResolvedValueOnce(new Response(JSON.stringify({ token: 'csrf-token' }), { status: 200 }))
      .mockResolvedValueOnce(new Response(JSON.stringify({ message: 'Acceso denegado' }), { status: 403 }))
    vi.stubGlobal('fetch', failingFetch)
    await expect(postJson('/admin')).rejects.toThrow('Acceso denegado')
  })
})
