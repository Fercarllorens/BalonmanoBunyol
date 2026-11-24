import { fireEvent, render, screen, waitFor } from '@testing-library/react'
import { describe, expect, it, vi } from 'vitest'
import TeamPage from './TeamPage'
import type { SessionUser } from '../system/SiteHeader'

vi.mock('../auth/authApi', () => ({ getCsrfToken: vi.fn().mockResolvedValue('csrf-token') }))

const user: SessionUser = { id: 'manager-1', displayName: 'Entrenadora', email: 'entrenadora@example.es', roles: ['CuerpoTecnico'] }
const firstTeam = { id: 'team-a', name: 'Infantil A', category: 'Infantil', access: 'CuerpoTecnico' }
const secondTeam = { id: 'team-b', name: 'Cadete B', category: 'Cadete', access: 'Jugador' }
const space = {
  team: firstTeam, canManage: true,
  players: [{ userId: 'player-1', displayName: 'Pau Pérez', email: 'pau@example.es', avatarUrl: null, jerseyNumber: 7, position: 'Extremo Derecho', matchesPlayed: 4, minutesPlayed: 80, goals: 12, saves: 0 }],
  matches: [], events: [], boardPosts: [],
}
const playerSpace = { ...space, team: { ...firstTeam, access: 'Jugador' }, canManage: false }
const administrator: SessionUser = { id: 'admin-1', displayName: 'Admin Buñol', email: 'admin@example.es', roles: ['Administrador'] }

function responseFor(url: string) {
  if (url === '/api/v1/admin/teams') return new Response(JSON.stringify([
    { id: 'team-a', name: 'Infantil A', category: 'Infantil', isActive: true },
    { id: 'team-b', name: 'Cadete B', category: 'Cadete', isActive: true },
    { id: 'team-off', name: 'Benjamín antiguo', category: 'Benjamín', isActive: false },
  ]), { status: 200 })
  if (url === '/api/v1/my-teams') return new Response(JSON.stringify([firstTeam, secondTeam]), { status: 200 })
  if (url.endsWith('/team-a/space')) return new Response(JSON.stringify(space), { status: 200 })
  if (url.endsWith('/team-b/space')) return new Response(JSON.stringify({ ...space, team: secondTeam }), { status: 200 })
  if (url.includes('/calendar?')) return new Response(JSON.stringify({ seasonStart: '2026-07-01', seasonEnd: '2027-06-30', month: '2026-11-01', items: [], plans: [] }), { status: 200 })
  if (url.includes('/players/search')) return new Response(JSON.stringify([]), { status: 200 })
  return new Response(JSON.stringify({}), { status: 200 })
}

describe('TeamPage', () => {
  it('muestra el selector solo si se tienen varios equipos y carga la selección nueva', async () => {
    const fetchMock = vi.fn().mockImplementation((input: RequestInfo | URL) => Promise.resolve(responseFor(String(input))))
    vi.stubGlobal('fetch', fetchMock)
    render(<TeamPage theme="light" onToggleTheme={() => undefined} user={user}/> )

    expect(await screen.findByRole('heading', { level: 1, name: 'Infantil A' })).toBeInTheDocument()
    fireEvent.click(screen.getByRole('button', { name: /Seleccionar equipo/ }))
    fireEvent.click(await screen.findByRole('option', { name: /Cadete B/ }))
    await waitFor(() => expect(fetchMock).toHaveBeenCalledWith('/api/v1/teams/team-b/space', expect.anything()))
    expect(screen.queryByRole('listbox', { name: 'Equipos disponibles' })).not.toBeInTheDocument()
  })

  it('muestra a Administración todos los equipos activos en el desplegable', async () => {
    const fetchMock = vi.fn().mockImplementation((input: RequestInfo | URL) => Promise.resolve(responseFor(String(input))))
    vi.stubGlobal('fetch', fetchMock)
    render(<TeamPage theme="light" onToggleTheme={() => undefined} user={administrator}/> )

    expect(await screen.findByRole('heading', { level: 1, name: 'Infantil A' })).toBeInTheDocument()
    expect(fetchMock).toHaveBeenCalledWith('/api/v1/admin/teams', expect.anything())
    fireEvent.click(screen.getByRole('button', { name: /Seleccionar equipo/ }))
    expect(await screen.findByRole('option', { name: /Cadete B/ })).toBeInTheDocument()
    expect(screen.queryByRole('option', { name: /Benjamín antiguo/ })).not.toBeInTheDocument()
  })

  it('permite al staff añadir un jugador con dorsal y posición', async () => {
    const fetchMock = vi.fn().mockImplementation((input: RequestInfo | URL) => {
      const url = String(input)
      if (url.includes('/players/search')) return Promise.resolve(new Response(JSON.stringify([{ userId: 'player-2', displayName: 'Ana Jugadora', email: 'ana@example.es' }]), { status: 200 }))
      return Promise.resolve(responseFor(url))
    })
    vi.stubGlobal('fetch', fetchMock)
    render(<TeamPage theme="light" onToggleTheme={() => undefined} user={user}/> )

    await screen.findByText('Pau Pérez')
    fireEvent.change(screen.getByRole('textbox', { name: 'Buscar jugador para añadir' }), { target: { value: 'Ana' } })
    fireEvent.click(await screen.findByRole('option', { name: /Ana Jugadora/ }))
    expect(screen.getByRole('button', { name: 'Quitar jugador seleccionado' })).toHaveTextContent('Ana Jugadora')
    fireEvent.change(screen.getByRole('spinbutton', { name: 'Dorsal' }), { target: { value: '14' } })
    fireEvent.click(screen.getByRole('button', { name: 'Añadir a plantilla' }))
    await waitFor(() => expect(fetchMock).toHaveBeenCalledWith('/api/v1/teams/team-a/players', expect.objectContaining({ method: 'POST' })))
  })

  it('permite seleccionar como jugador una cuenta con rol de Cuerpo Técnico', async () => {
    const fetchMock = vi.fn().mockImplementation((input: RequestInfo | URL, init?: RequestInit) => {
      const url = String(input)
      if (url.includes('/players/search')) return Promise.resolve(new Response(JSON.stringify([{ userId: 'coach-2', displayName: 'Irene Entrenadora', email: 'irene@example.es' }]), { status: 200 }))
      if (url.endsWith('/players') && init?.method === 'POST') return Promise.resolve(new Response(null, { status: 204 }))
      return Promise.resolve(responseFor(url))
    })
    vi.stubGlobal('fetch', fetchMock)
    render(<TeamPage theme="light" onToggleTheme={() => undefined} user={user}/> )

    const search = await screen.findByRole('textbox', { name: 'Buscar jugador para añadir' })
    fireEvent.change(search, { target: { value: 'Irene' } })
    fireEvent.click(await screen.findByRole('option', { name: /Irene Entrenadora/ }))
    expect(screen.getByRole('button', { name: 'Quitar jugador seleccionado' })).toHaveTextContent('Irene Entrenadora')
    fireEvent.click(screen.getByRole('button', { name: 'Añadir a plantilla' }))

    await waitFor(() => expect(fetchMock).toHaveBeenCalledWith('/api/v1/teams/team-a/players', expect.objectContaining({
      method: 'POST',
      body: JSON.stringify({ userId: 'coach-2', jerseyNumber: null, position: 'Universal' }),
    })))
  })

  it('muestra candidatos como lista superpuesta y permite navegar con teclado', async () => {
    const fetchMock = vi.fn().mockImplementation((input: RequestInfo | URL) => {
      const url = String(input)
      if (url.includes('/players/search')) return Promise.resolve(new Response(JSON.stringify([
        { userId: 'player-2', displayName: 'Ana Jugadora', email: 'ana@example.es' },
        { userId: 'player-3', displayName: 'Ane Jugadora', email: 'ane@example.es' },
      ]), { status: 200 }))
      return Promise.resolve(responseFor(url))
    })
    vi.stubGlobal('fetch', fetchMock)
    render(<TeamPage theme="light" onToggleTheme={() => undefined} user={user}/> )

    const search = await screen.findByRole('textbox', { name: 'Buscar jugador para añadir' })
    fireEvent.change(search, { target: { value: 'Ana' } })
    const candidates = await screen.findByRole('listbox', { name: 'Jugadores encontrados' })
    expect(await screen.findByRole('option', { name: /Ana Jugadora/ })).toBeInTheDocument()
    expect(candidates.parentElement).toHaveClass('team-search-wrap')
    expect(screen.getByRole('spinbutton', { name: 'Dorsal' })).toBeVisible()
    expect(screen.getByRole('button', { name: 'Añadir a plantilla' })).toBeVisible()

    fireEvent.keyDown(search, { key: 'ArrowDown' })
    fireEvent.keyDown(search, { key: 'Enter' })
    expect(screen.queryByRole('listbox', { name: 'Jugadores encontrados' })).not.toBeInTheDocument()
    expect(screen.getByRole('button', { name: 'Quitar jugador seleccionado' })).toHaveTextContent('Ana Jugadora')
  })

  it('cierra la lista de candidatos con Escape', async () => {
    vi.stubGlobal('fetch', vi.fn().mockImplementation((input: RequestInfo | URL) => {
      const url = String(input)
      if (url.includes('/players/search')) return Promise.resolve(new Response(JSON.stringify([{ userId: 'player-2', displayName: 'Ana Jugadora', email: 'ana@example.es' }]), { status: 200 }))
      return Promise.resolve(responseFor(url))
    }))
    render(<TeamPage theme="light" onToggleTheme={() => undefined} user={user}/> )

    const search = await screen.findByRole('textbox', { name: 'Buscar jugador para añadir' })
    fireEvent.change(search, { target: { value: 'Ana' } })
    await screen.findByRole('listbox', { name: 'Jugadores encontrados' })
    fireEvent.keyDown(search, { key: 'Escape' })
    expect(screen.queryByRole('listbox', { name: 'Jugadores encontrados' })).not.toBeInTheDocument()
  })

  it('limpia consulta y sugerencias al cambiar de equipo', async () => {
    const fetchMock = vi.fn().mockImplementation((input: RequestInfo | URL) => {
      const url = String(input)
      if (url.includes('/players/search')) return Promise.resolve(new Response(JSON.stringify([{ userId: 'player-2', displayName: 'Ana Jugadora', email: 'ana@example.es' }]), { status: 200 }))
      return Promise.resolve(responseFor(url))
    })
    vi.stubGlobal('fetch', fetchMock)
    render(<TeamPage theme="light" onToggleTheme={() => undefined} user={user}/> )

    await screen.findByText('Pau Pérez')
    const search = screen.getByRole('textbox', { name: 'Buscar jugador para añadir' })
    fireEvent.change(search, { target: { value: 'Ana' } })
    fireEvent.click(await screen.findByRole('option', { name: /Ana Jugadora/ }))
    expect(screen.getByRole('button', { name: 'Quitar jugador seleccionado' })).toBeInTheDocument()

    fireEvent.click(screen.getByRole('button', { name: /Seleccionar equipo/ }))
    fireEvent.click(await screen.findByRole('option', { name: /Cadete B/ }))

    await waitFor(() => expect(fetchMock).toHaveBeenCalledWith('/api/v1/teams/team-b/space', expect.anything()))
    expect(screen.queryByRole('button', { name: 'Quitar jugador seleccionado' })).not.toBeInTheDocument()
    expect(search).toHaveValue('')
  })

  it('los jugadores ven la plantilla pero no las acciones de gestión', async () => {
    vi.stubGlobal('fetch', vi.fn().mockImplementation((input: RequestInfo | URL) => {
      const url = String(input)
      return Promise.resolve(url.includes('/space') ? new Response(JSON.stringify(playerSpace), { status: 200 }) : responseFor(url))
    }))
    render(<TeamPage theme="dark" onToggleTheme={() => undefined} user={user}/> )
    await screen.findByText('Pau Pérez')
    expect(screen.queryByRole('button', { name: 'Añadir a plantilla' })).not.toBeInTheDocument()
    expect(screen.getByText('Pau Pérez')).toBeInTheDocument()
  })

  it('previsualiza imágenes y vídeos de la pizarra y mantiene los documentos como enlaces', async () => {
    const boardSpace = {
      ...space,
      boardPosts: [{
        id: 'post-1', authorId: 'manager-1', authorName: 'Entrenadora', content: 'Vídeo de la sesión', createdAt: '2026-01-01T10:00:00Z',
        attachments: [
          { id: 'image-1', fileName: 'táctica.jpg', contentType: 'image/jpeg', length: 1024, url: '/api/v1/teams/team-a/board-files/image-1' },
          { id: 'video-1', fileName: 'entrenamiento.mp4', contentType: 'video/mp4', length: 2048, url: '/api/v1/teams/team-a/board-files/video-1' },
          { id: 'pdf-1', fileName: 'ejercicios.pdf', contentType: 'application/pdf', length: 512, url: '/api/v1/teams/team-a/board-files/pdf-1' },
        ],
      }],
    }
    vi.stubGlobal('fetch', vi.fn().mockImplementation((input: RequestInfo | URL) => {
      const url = String(input)
      if (url.endsWith('/team-a/space')) return Promise.resolve(new Response(JSON.stringify(boardSpace), { status: 200 }))
      return Promise.resolve(responseFor(url))
    }))
    render(<TeamPage theme="light" onToggleTheme={() => undefined} user={user}/> )

    await screen.findByText('Pau Pérez')
    fireEvent.click(screen.getByRole('button', { name: 'Pizarra' }))

    const image = await screen.findByRole('img', { name: 'táctica.jpg' })
    expect(image).toHaveAttribute('src', '/api/v1/teams/team-a/board-files/image-1')
    expect(image.closest('a')).toHaveAttribute('target', '_blank')
    expect(document.querySelector('video[aria-label="Vídeo: entrenamiento.mp4"]')).toHaveAttribute('controls')
    expect(screen.getByRole('link', { name: 'ejercicios.pdf' })).toHaveAttribute('href', '/api/v1/teams/team-a/board-files/pdf-1')
  })

  it('muestra el calendario mensual y la temporada automática, y permite navegar de mes', async () => {
    const trainingDay = new Date(2026, 10, 2)
    const previewDate = `${trainingDay.getFullYear()}-${String(trainingDay.getMonth() + 1).padStart(2, '0')}-${String(trainingDay.getDate()).padStart(2, '0')}`
    const fetchMock = vi.fn().mockImplementation((input: RequestInfo | URL) => {
      const url = String(input)
      if (url.includes('/calendar?')) {
        const month = new URL(url, 'http://localhost').searchParams.get('month') ?? '11'
        return Promise.resolve(new Response(JSON.stringify({
          seasonStart: '2026-07-01', seasonEnd: '2027-06-30', month: `2026-${month.padStart(2, '0')}-01`, items: [{
            id: 'training-1', source: 'Planificacion', kind: 'Entrenamiento', title: 'Entrenamiento',
            startsAt: `${previewDate}T19:00:00Z`, endsAt: `${previewDate}T20:30:00Z`, venue: '', notes: '',
            isCancelled: false, trainingSlotId: 'slot-1', trainingDate: previewDate, canManage: false, callups: null,
          }], plans: [],
        }), { status: 200 }))
      }
      return Promise.resolve(responseFor(url))
    })
    vi.stubGlobal('fetch', fetchMock)
    render(<TeamPage theme="light" onToggleTheme={() => undefined} user={user}/> )

    await screen.findByText('Pau Pérez')
    fireEvent.click(screen.getByRole('button', { name: 'Calendario' }))
    expect(await screen.findByRole('grid', { name: /Calendario de/ })).toBeInTheDocument()
    expect(screen.getByRole('button', { name: 'Mes anterior' })).toBeEnabled()
    const previewTitle = await waitFor(() => {
      const preview = document.querySelector('.team-calendar-preview-item')
      expect(preview).toBeInTheDocument()
      return preview
    })
    expect(previewTitle).toHaveTextContent('Entrenamiento')
    const dayCell = previewTitle?.closest('[role="gridcell"]')
    expect(dayCell).toHaveAttribute('aria-label', expect.stringContaining('actividades'))
    expect(dayCell).toHaveTextContent(/\d{2}:\d{2}/)
    fireEvent.click(screen.getByRole('button', { name: 'Mes anterior' }))
    await waitFor(() => expect(fetchMock).toHaveBeenCalledWith('/api/v1/teams/team-a/calendar?year=2026&month=10', expect.anything()))
  })

  it('presenta la cancelación de una sesión como una opción compacta y accesible', async () => {
    const trainingDay = new Date(2026, 10, 2)
    const trainingDate = `${trainingDay.getFullYear()}-${String(trainingDay.getMonth() + 1).padStart(2, '0')}-${String(trainingDay.getDate()).padStart(2, '0')}`
    vi.stubGlobal('fetch', vi.fn().mockImplementation((input: RequestInfo | URL) => {
      const url = String(input)
      if (url.includes('/calendar?')) return Promise.resolve(new Response(JSON.stringify({
        seasonStart: '2026-07-01', seasonEnd: '2027-06-30', month: '2026-11-01', plans: [], items: [{
          id: 'training:slot-1:20261102', source: 'Planificacion', kind: 'Entrenamiento', title: 'Entrenamiento',
          startsAt: `${trainingDate}T18:00:00Z`, endsAt: `${trainingDate}T19:30:00Z`, venue: '', notes: '', isCancelled: false,
          trainingSlotId: 'slot-1', trainingDate, canManage: true, callups: null,
        }],
      }), { status: 200 }))
      return Promise.resolve(responseFor(url))
    }))
    render(<TeamPage theme="light" onToggleTheme={() => undefined} user={user}/> )

    await screen.findByText('Pau Pérez')
    fireEvent.click(screen.getByRole('button', { name: 'Calendario' }))
    await waitFor(() => expect(document.querySelector('.team-calendar-preview-item')).toBeInTheDocument())
    fireEvent.click(screen.getByRole('button', { name: 'Editar fecha' }))

    expect(await screen.findByRole('heading', { name: 'Modificar entrenamiento' })).toBeInTheDocument()
    expect(screen.getByText('El cambio solo afectará a esta fecha.')).toBeInTheDocument()
    const cancelOption = screen.getByText('Cancelar este entrenamiento').closest('label')
    expect(cancelOption).toHaveClass('team-training-cancel-option')
    expect(cancelOption?.querySelector('input[type="checkbox"]')).toBeInTheDocument()
    expect(screen.getByRole('button', { name: 'Guardar cambios' })).toBeInTheDocument()
  })

  it('oculta el selector de equipo cuando solo se tiene uno', async () => {
    vi.stubGlobal('fetch', vi.fn().mockImplementation((input: RequestInfo | URL) => {
      if (String(input) === '/api/v1/my-teams') return Promise.resolve(new Response(JSON.stringify([firstTeam]), { status: 200 }))
      return Promise.resolve(responseFor(String(input)))
    }))
    render(<TeamPage theme="light" onToggleTheme={() => undefined} user={user}/> )
    expect(await screen.findByRole('heading', { level: 1, name: 'Infantil A' })).toBeInTheDocument()
    expect(screen.queryByRole('button', { name: /Seleccionar equipo/ })).not.toBeInTheDocument()
  })
})
