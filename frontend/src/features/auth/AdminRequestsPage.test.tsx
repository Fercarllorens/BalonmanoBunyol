import { fireEvent, render, screen, waitFor } from '@testing-library/react'
import { describe, expect, it, vi } from 'vitest'
import type { SessionUser } from '../system/SiteHeader'
import AdminRequestsPage, { type RoleRequest } from './AdminRequestsPage'

vi.mock('./authApi', () => ({ postJson: vi.fn() }))

import { postJson } from './authApi'

const user: SessionUser = { id: 'admin-id', email: 'admin@example.es', displayName: 'Administración', roles: ['Administrador'] }

describe('AdminRequestsPage', () => {
  it('muestra solicitudes pendientes y resuelve con aprobación', async () => {
    const request: RoleRequest = {
      id: 'request-id', displayName: 'Pau Pérez', email: 'pau@example.es',
      requestedRole: 'CuerpoTecnico', status: 'Pending', createdAt: '2026-01-01T10:00:00Z',
    }
    vi.mocked(postJson).mockResolvedValueOnce(undefined)
    vi.stubGlobal('fetch', vi.fn())
    render(<AdminRequestsPage theme="light" onToggleTheme={() => undefined} user={user} initialRequests={[request]}/>)

    expect(screen.getByText('1 pendientes')).toBeInTheDocument()
    expect(screen.getByText('Cuerpo Técnico')).toBeInTheDocument()
    fireEvent.click(screen.getByRole('button', { name: 'Aprobar' }))

    await waitFor(() => expect(postJson).toHaveBeenCalledWith('/api/v1/admin/role-requests/request-id/approve'))
  })

  it('presenta errores al resolver una solicitud', async () => {
    const request: RoleRequest = {
      id: 'request-id', displayName: 'Pau Pérez', email: 'pau@example.es',
      requestedRole: 'Jugador', status: 'Pending', createdAt: '2026-01-01T10:00:00Z',
    }
    vi.mocked(postJson).mockRejectedValueOnce(new Error('No autorizado'))
    vi.stubGlobal('fetch', vi.fn())
    render(<AdminRequestsPage theme="light" onToggleTheme={() => undefined} user={user} initialRequests={[request]}/>)
    fireEvent.click(screen.getByRole('button', { name: 'Rechazar' }))

    expect(await screen.findByRole('alert')).toHaveTextContent('No autorizado')
  })

  it('carga solicitudes cuando no se proporcionan datos iniciales', async () => {
    vi.stubGlobal('fetch', vi.fn().mockResolvedValue(new Response(JSON.stringify([]), { status: 200 })))
    render(<AdminRequestsPage theme="light" onToggleTheme={() => undefined} user={user}/>)

    expect(await screen.findByText('No hay solicitudes por revisar.')).toBeInTheDocument()
  })

  it('muestra acceso denegado y permite reintentar la carga', async () => {
    const fetchMock = vi.fn()
      .mockResolvedValueOnce(new Response(null, { status: 403 }))
      .mockResolvedValueOnce(new Response(JSON.stringify([]), { status: 200 }))
    vi.stubGlobal('fetch', fetchMock)
    render(<AdminRequestsPage theme="light" onToggleTheme={() => undefined} user={user}/>)

    expect(await screen.findByRole('alert')).toHaveTextContent('Tu cuenta no tiene permisos de administración.')
    fireEvent.click(screen.getByRole('button', { name: 'Reintentar' }))
    expect(await screen.findByText('No hay solicitudes por revisar.')).toBeInTheDocument()
  })
})
