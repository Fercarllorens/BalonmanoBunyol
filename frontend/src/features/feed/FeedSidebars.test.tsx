import { fireEvent, render, screen, waitFor, within } from '@testing-library/react'
import { beforeEach, describe, expect, it, vi } from 'vitest'
import type { SessionUser } from '../system/SiteHeader'
import PublicFeedPage from './PublicFeedPage'

const mocks = vi.hoisted(() => ({ getCsrfToken: vi.fn() }))

vi.mock('../auth/authApi', () => ({ getCsrfToken: mocks.getCsrfToken }))

describe('PublicFeedPage sidebars', () => {
  beforeEach(() => {
    mocks.getCsrfToken.mockReset().mockResolvedValue('csrf-token')
    vi.stubGlobal('fetch', vi.fn().mockImplementation((input: RequestInfo | URL) => {
      const url = String(input)
      if (url.includes('/api/v1/feed')) return Promise.resolve(new Response(JSON.stringify({ posts: [], hasMore: false, nextBefore: null, nextBeforeId: null }), { status: 200 }))
      return Promise.resolve(new Response(null, { status: 204 }))
    }))
  })

  it('muestra la cuenta real y el panel de competición una sola vez', () => {
    const user: SessionUser = { id: 'user-1', displayName: 'Ana Jugadora', email: 'ana@example.es', roles: ['Jugador'] }
    render(<PublicFeedPage theme="light" onToggleTheme={() => undefined} user={user}/>)

    expect(screen.getAllByText('Ana Jugadora')).toHaveLength(2)
    expect(screen.getByText('ana@example.es')).toBeInTheDocument()
    expect(screen.getByText('Próxima jornada')).toBeInTheDocument()
    expect(screen.getByText('Clasificación')).toBeInTheDocument()
    expect(screen.getAllByText('DEMO')).toHaveLength(2)

    const leftRail = document.querySelector('.feed-rail:not(.feed-rail-right)')
    const rightRail = document.querySelector('.feed-rail-right')
    expect(leftRail).toBeInTheDocument()
    expect(rightRail).toBeInTheDocument()
    expect(within(leftRail as HTMLElement).queryByText('Clasificación')).not.toBeInTheDocument()
    expect(within(rightRail as HTMLElement).queryByText('Club Directo')).not.toBeInTheDocument()
  })

  it('mantiene los laterales previstos para una visita pública', () => {
    render(<PublicFeedPage theme="dark" onToggleTheme={() => undefined} user={null}/>)

    expect(screen.getByText('Comunidad del club')).toBeInTheDocument()
    expect(screen.getByText('Visitante')).toBeInTheDocument()
    expect(screen.getByText('Encuentro de ejemplo, no oficial')).toBeInTheDocument()
    expect(screen.getByText('Clasificación de demostración')).toBeInTheDocument()
  })

  it('publica una entrada de texto y limpia el formulario tras confirmación', async () => {
    const user: SessionUser = { id: 'user-1', displayName: 'Ana Jugadora', email: 'ana@example.es', roles: ['Jugador'] }
    const fetchMock = vi.mocked(fetch)
    fetchMock.mockImplementation((input: RequestInfo | URL, init?: RequestInit) => {
      if (String(input) === '/api/v1/feed' && init?.method === 'POST') {
        return Promise.resolve(new Response(JSON.stringify({ id: 'new-post' }), { status: 201 }))
      }
      return Promise.resolve(new Response(JSON.stringify({ posts: [], hasMore: false, nextBefore: null, nextBeforeId: null }), { status: 200 }))
    })
    render(<PublicFeedPage theme="light" onToggleTheme={() => undefined} user={user}/>)
    const composer = screen.getByLabelText('Texto de la publicación')
    fireEvent.change(composer, { target: { value: '¡Amunt Buñol!' } })
    fireEvent.click(screen.getByRole('button', { name: /Publicar/ }))

    await waitFor(() => expect(fetchMock).toHaveBeenCalledWith('/api/v1/feed', expect.objectContaining({ method: 'POST' })))
    await waitFor(() => expect(composer).toHaveValue(''))
    expect(await screen.findByRole('status')).toHaveTextContent('Tu publicación se ha compartido en el feed general.')
  })

  it('rechaza la publicación vacía localmente', async () => {
    const user: SessionUser = { id: 'user-1', displayName: 'Ana Jugadora', email: 'ana@example.es', roles: ['Jugador'] }
    const fetchMock = vi.mocked(fetch)
    render(<PublicFeedPage theme="light" onToggleTheme={() => undefined} user={user}/>)
    fireEvent.click(screen.getByRole('button', { name: /Publicar/ }))

    expect(await screen.findByRole('alert')).toHaveTextContent('Escribe un mensaje o adjunta una imagen o vídeo.')
    expect(fetchMock).not.toHaveBeenCalledWith('/api/v1/feed', expect.objectContaining({ method: 'POST' }))
  })

  it('valida el formato de archivo antes de enviar la publicación', async () => {
    const user: SessionUser = { id: 'user-1', displayName: 'Ana Jugadora', email: 'ana@example.es', roles: ['Jugador'] }
    const fetchMock = vi.mocked(fetch)
    render(<PublicFeedPage theme="light" onToggleTheme={() => undefined} user={user}/>)
    const fileInput = document.querySelector('input[type="file"]') as HTMLInputElement
    const invalidFile = new File(['contenido'], 'documento.pdf', { type: 'application/pdf' })
    fireEvent.change(fileInput, { target: { files: [invalidFile] } })

    expect(await screen.findByRole('alert')).toHaveTextContent('Formatos admitidos: JPEG, PNG, WebP, MP4 y WebM.')
    expect(fetchMock).not.toHaveBeenCalledWith('/api/v1/feed', expect.objectContaining({ method: 'POST' }))
  })

  it('muestra la foto de perfil del autor en las publicaciones del feed', async () => {
    const authorAvatarUrl = '/api/v1/feed/author-avatar/author-1'
    vi.stubGlobal('fetch', vi.fn().mockImplementation((input: RequestInfo | URL) => {
      if (String(input).includes('/api/v1/feed')) {
        return Promise.resolve(new Response(JSON.stringify({
          posts: [{
            id: 'post-1', authorId: 'author-1', authorDisplayName: 'Ana Jugadora', authorAvatarUrl,
            authorProfile: 'Jugador/a', content: '¡Amunt!', createdAt: '2026-01-01T10:00:00Z', attachments: [],
          }],
          hasMore: false, nextBefore: null, nextBeforeId: null,
        }), { status: 200 }))
      }
      return Promise.resolve(new Response(null, { status: 204 }))
    }))

    render(<PublicFeedPage theme="light" onToggleTheme={() => undefined} user={null}/>)
    const author = await screen.findByText('¡Amunt!')
    const post = author.closest('.feed-post')
    expect(post?.querySelector('.post-avatar img')).toHaveAttribute('src', authorAvatarUrl)
  })
})
