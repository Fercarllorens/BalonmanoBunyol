import { fireEvent, render, screen, waitFor } from '@testing-library/react'
import { describe, expect, it, vi } from 'vitest'
import type { SessionUser } from '../system/SiteHeader'
import AdministrationPage from './AdministrationPage'

vi.mock('../auth/authApi', () => ({ postJson: vi.fn(), putJson: vi.fn() }))

import { postJson, putJson } from '../auth/authApi'

const administrator: SessionUser = { id: 'admin-id', email: 'admin@example.es', displayName: 'Admin Buñol', roles: ['Administrador'] }
const team = { id: 'team-1', name: 'Infantil Mixto', category: 'Infantil', isActive: true }
const seniorTeam = { id: 'team-2', name: 'Senior A', category: 'Senior', isActive: true }
const inactiveTeam = { id: 'team-3', name: 'Cadete B', category: 'Cadete', isActive: false }
const coach = { id: 'coach-id', displayName: 'Pau Pérez', email: 'pau@example.es', roles: ['CuerpoTecnico'], teams: [team, inactiveTeam] }
const secondCoach = { id: 'coach-2', displayName: 'Irene Torres', email: 'irene@example.es', roles: ['CuerpoTecnico'], teams: [] }
const assignedAdministrator = { id: 'assigned-admin-id', displayName: 'Admin Buñol', email: 'admin@example.es', roles: ['Administrador'], teams: [seniorTeam] }
const request = { id: 'request-id', userId: 'player-id', displayName: 'Ana Jugadora', email: 'ana@example.es', requestedRole: 'Jugador', status: 'Pending', createdAt: '2026-01-01T10:00:00Z' }

function fetchResponse(url: string) {
  if (url.includes('/role-requests')) return new Response(JSON.stringify([request]), { status: 200 })
  if (url.endsWith('/teams')) return new Response(JSON.stringify([team, seniorTeam, inactiveTeam]), { status: 200 })
  if (url.includes('/users?') && url.includes('coachesOnly=true')) return new Response(JSON.stringify([coach, assignedAdministrator]), { status: 200 })
  if (url.includes('/users?')) return new Response(JSON.stringify([coach]), { status: 200 })
  return new Response(JSON.stringify([]), { status: 200 })
}

describe('AdministrationPage', () => {
  it('muestra las solicitudes pendientes y sustituye el rol al aprobar', async () => {
    vi.stubGlobal('fetch', vi.fn().mockImplementation((url: string) => Promise.resolve(fetchResponse(url))))
    vi.mocked(postJson).mockResolvedValueOnce(undefined)
    render(<AdministrationPage theme="light" onToggleTheme={() => undefined} user={administrator}/> )

    expect(await screen.findByText('Ana Jugadora')).toBeInTheDocument()
    fireEvent.click(screen.getByRole('button', { name: 'Aprobar' }))
    await waitFor(() => expect(postJson).toHaveBeenCalledWith('/api/v1/admin/role-requests/request-id/approve'))
  })

  it('muestra las asignaciones actuales y permite añadir equipos con el selector buscable', async () => {
    vi.stubGlobal('fetch', vi.fn().mockImplementation((url: string) => {
      const path = String(url)
      if (path.includes('/users?') && path.includes('coachesOnly=true')) return Promise.resolve(new Response(JSON.stringify([coach]), { status: 200 }))
      return Promise.resolve(fetchResponse(path))
    }))
    vi.mocked(putJson).mockResolvedValueOnce(undefined)
    render(<AdministrationPage theme="light" onToggleTheme={() => undefined} user={administrator}/> )

    fireEvent.click(screen.getByRole('button', { name: /Entrenadores y equipos/ }))
    expect(await screen.findByText('Pau Pérez')).toBeInTheDocument()
    expect(document.querySelectorAll('.admin-coach-card')).toHaveLength(1)
    expect(document.querySelector('.admin-coach-card')).toHaveTextContent('Pau Pérez')
    expect(screen.getByRole('button', { name: /Equipos de Pau Pérez: 2 asignados/ })).toHaveAttribute('aria-expanded', 'false')
    expect(screen.queryByRole('checkbox', { name: 'Asignar Infantil Mixto a Pau Pérez' })).not.toBeInTheDocument()
    fireEvent.click(screen.getByRole('button', { name: /Equipos de Pau Pérez: 2 asignados/ }))
    expect(screen.getByText('2 seleccionados')).toBeInTheDocument()
    expect(screen.getAllByText('Infantil Mixto').length).toBeGreaterThan(0)
    expect(screen.getAllByText('Cadete B').length).toBeGreaterThan(0)
    expect(screen.getByRole('checkbox', { name: 'Asignar Infantil Mixto a Pau Pérez' })).toBeChecked()
    expect(screen.getByRole('checkbox', { name: 'Asignar Cadete B a Pau Pérez' })).toBeChecked()
    expect(screen.getByRole('checkbox', { name: 'Asignar Cadete B a Pau Pérez' }).closest('label')).toHaveTextContent('Inactivo')
    fireEvent.change(screen.getByRole('textbox', { name: 'Buscar entrenadores' }), { target: { value: 'Pau' } })
    expect(await screen.findByText('Pau Pérez')).toBeInTheDocument()
    fireEvent.change(screen.getByRole('textbox', { name: 'Buscar equipos de Pau Pérez' }), { target: { value: 'Senior' } })
    const seniorAssignment = screen.getByRole('checkbox', { name: 'Asignar Senior A a Pau Pérez' })
    expect(seniorAssignment).not.toBeChecked()
    fireEvent.click(seniorAssignment)
    expect(screen.getByText('3 seleccionados')).toBeInTheDocument()
    fireEvent.click(screen.getByRole('button', { name: 'Guardar equipos' }))
    await waitFor(() => expect(putJson).toHaveBeenCalledWith('/api/v1/admin/users/coach-id/teams', { teamIds: ['team-1', 'team-3', 'team-2'] }))
  })

  it('muestra guardado solo en la cuenta cuya asignación se está enviando', async () => {
    vi.stubGlobal('fetch', vi.fn().mockImplementation((url: string) => {
      const path = String(url)
      if (path.includes('/users?') && path.includes('coachesOnly=true')) return Promise.resolve(new Response(JSON.stringify([coach, secondCoach]), { status: 200 }))
      return Promise.resolve(fetchResponse(path))
    }))
    let finishSave: () => void = () => undefined
    vi.mocked(putJson).mockImplementationOnce(() => new Promise<void>((resolve) => { finishSave = resolve }))
    render(<AdministrationPage theme="light" onToggleTheme={() => undefined} user={administrator}/> )

    fireEvent.click(screen.getByRole('button', { name: /Entrenadores y equipos/ }))
    await screen.findByText('Irene Torres')

    fireEvent.click(screen.getByRole('button', { name: /Equipos de Pau Pérez: 2 asignados/ }))
    fireEvent.click(screen.getByRole('checkbox', { name: 'Asignar Infantil Mixto a Pau Pérez' }))
    fireEvent.click(screen.getByRole('button', { name: /Equipos de Pau Pérez: 1 asignado/ }))

    fireEvent.click(screen.getByRole('button', { name: /Equipos de Irene Torres: 0 asignados/ }))
    fireEvent.click(screen.getByRole('checkbox', { name: 'Asignar Senior A a Irene Torres' }))
    fireEvent.click(screen.getByRole('button', { name: /Equipos de Irene Torres: 1 asignados/ }))

    const saveButtons = screen.getAllByRole('button', { name: 'Guardar equipos' })
    fireEvent.click(saveButtons[0])
    await waitFor(() => expect(putJson).toHaveBeenCalledWith('/api/v1/admin/users/coach-id/teams', { teamIds: ['team-3'] }))

    expect(await screen.findByRole('button', { name: 'Guardando…' })).toBeInTheDocument()
    expect(screen.getByRole('button', { name: 'Guardar equipos' })).toBeEnabled()
    finishSave()
    await waitFor(() => expect(screen.queryByRole('button', { name: 'Guardando…' })).not.toBeInTheDocument())
  })

  it('busca cuentas y cambia el rol principal', async () => {
    vi.stubGlobal('fetch', vi.fn().mockImplementation((url: string) => Promise.resolve(fetchResponse(String(url)))))
    vi.mocked(putJson).mockResolvedValueOnce(undefined)
    render(<AdministrationPage theme="light" onToggleTheme={() => undefined} user={administrator}/> )

    fireEvent.click(screen.getByRole('button', { name: 'Usuarios y roles' }))
    fireEvent.change(await screen.findByRole('textbox', { name: 'Buscar usuarios' }), { target: { value: 'Pau' } })
    expect(await screen.findByText('Pau Pérez')).toBeInTheDocument()
    fireEvent.change(screen.getByRole('combobox', { name: 'Rol de Pau Pérez' }), { target: { value: 'Jugador' } })
    await waitFor(() => expect(putJson).toHaveBeenCalledWith('/api/v1/admin/users/coach-id/role', { role: 'Jugador' }))
  })

  it('crea un equipo con una categoría sin asociarlo a una temporada', async () => {
    vi.stubGlobal('fetch', vi.fn().mockImplementation((url: string) => Promise.resolve(fetchResponse(String(url)))))
    vi.mocked(postJson).mockResolvedValueOnce(undefined)
    render(<AdministrationPage theme="light" onToggleTheme={() => undefined} user={administrator}/> )

    fireEvent.click(screen.getByRole('button', { name: 'Equipos' }))
    fireEvent.change(await screen.findByRole('textbox', { name: 'Nombre del equipo' }), { target: { value: 'Senior A' } })
    fireEvent.click(screen.getByRole('button', { name: 'Crear equipo' }))
    expect(screen.getByRole('option', { name: 'Senior' })).toBeInTheDocument()
    await waitFor(() => expect(postJson).toHaveBeenCalledWith('/api/v1/admin/teams', { name: 'Senior A', category: 'Senior' }))
  })

  it('carga y modifica la plantilla del equipo desde el panel de Administración', async () => {
    const fetchMock = vi.fn().mockImplementation((input: RequestInfo | URL) => {
      const url = String(input)
      if (url.includes('/team-1/players')) return Promise.resolve(new Response(JSON.stringify([]), { status: 200 }))
      return Promise.resolve(fetchResponse(url))
    })
    vi.stubGlobal('fetch', fetchMock)
    vi.mocked(putJson).mockResolvedValueOnce(undefined)
    render(<AdministrationPage theme="light" onToggleTheme={() => undefined} user={administrator}/> )

    fireEvent.click(screen.getByRole('button', { name: 'Equipos' }))
    fireEvent.click(await screen.findByRole('button', { name: 'Plantilla · Infantil Mixto' }))
    await screen.findByText(/Plantilla · Infantil Mixto/)
    expect(fetchMock).toHaveBeenCalledWith('/api/v1/admin/teams/team-1/players', expect.anything())
  })

  it('incluye cuentas de Cuerpo Técnico en la búsqueda de personas para una plantilla', async () => {
    const fetchMock = vi.fn().mockImplementation((input: RequestInfo | URL) => {
      const url = String(input)
      if (url.includes('/users?query=Irene') && url.includes('playersOnly=true')) return Promise.resolve(new Response(JSON.stringify([
        { id: 'coach-2', displayName: 'Irene Torres', email: 'irene@example.es', roles: ['CuerpoTecnico'], teams: [] },
      ]), { status: 200 }))
      if (url.includes('/team-1/players')) return Promise.resolve(new Response(JSON.stringify([]), { status: 200 }))
      return Promise.resolve(fetchResponse(url))
    })
    vi.stubGlobal('fetch', fetchMock)
    render(<AdministrationPage theme="light" onToggleTheme={() => undefined} user={administrator}/> )

    fireEvent.click(screen.getByRole('button', { name: 'Equipos' }))
    fireEvent.click(await screen.findByRole('button', { name: 'Plantilla · Infantil Mixto' }))
    fireEvent.change(await screen.findByRole('textbox', { name: 'Buscar jugador de plantilla' }), { target: { value: 'Irene' } })

    expect(await screen.findByRole('option', { name: 'Irene Torres · irene@example.es · Cuerpo Técnico' })).toBeInTheDocument()
    expect(fetchMock).toHaveBeenCalledWith('/api/v1/admin/users?query=Irene&playersOnly=true', expect.anything())
  })
})
