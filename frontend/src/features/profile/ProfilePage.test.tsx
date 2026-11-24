import { fireEvent, render, screen, waitFor } from '@testing-library/react'
import { describe, expect, it, vi } from 'vitest'
import type { SessionUser } from '../system/SiteHeader'
import ProfilePage from './ProfilePage'

vi.mock('../auth/authApi', () => ({
  deleteRequest: vi.fn(),
  postForm: vi.fn(),
  putJson: vi.fn(),
}))

import { postForm, putJson } from '../auth/authApi'

const user: SessionUser = { id: 'user-id', displayName: 'Ana Pruebas', email: 'ana@example.es', roles: ['SocioAficion'] }
const profile = {
  ...user,
  firstName: 'Ana',
  lastName: 'Pruebas',
  preferredTeamId: 'senior-a-id',
  avatarUrl: null,
  availableTeams: [
    { id: 'senior-a-id', name: 'Senior A', category: 'Senior' },
    { id: 'infantil-id', name: 'Infantil A', category: 'Infantil' },
  ],
}

describe('ProfilePage', () => {
  it('muestra la preferencia de equipo concreta y los equipos activos disponibles', async () => {
    vi.stubGlobal('fetch', vi.fn().mockResolvedValue(new Response(JSON.stringify(profile), { status: 200 })))
    render(<ProfilePage theme="light" onToggleTheme={() => undefined} user={user} onProfileUpdated={() => undefined}/> )

    expect(await screen.findByRole('combobox', { name: 'Equipo preferido' })).toHaveValue('senior-a-id')
    expect(screen.getByRole('option', { name: 'Senior A · Senior' })).toBeInTheDocument()
    expect(screen.getByRole('option', { name: 'Infantil A · Infantil' })).toBeInTheDocument()
  })

  it('guarda los datos del perfil y notifica el cambio de sesión', async () => {
    vi.stubGlobal('fetch', vi.fn().mockResolvedValue(new Response(JSON.stringify(profile), { status: 200 })))
    const updated = { ...profile, firstName: 'Ana María', displayName: 'Ana María Pruebas', email: 'nueva@example.es', preferredTeamId: 'infantil-id' }
    vi.mocked(putJson).mockResolvedValueOnce(updated)
    const onProfileUpdated = vi.fn()
    render(<ProfilePage theme="light" onToggleTheme={() => undefined} user={user} onProfileUpdated={onProfileUpdated}/> )

    await screen.findByDisplayValue('Ana')
    fireEvent.change(screen.getByLabelText('Nombre'), { target: { value: 'Ana María' } })
    fireEvent.change(screen.getByDisplayValue('ana@example.es'), { target: { value: 'nueva@example.es' } })
    fireEvent.change(screen.getByRole('combobox', { name: 'Equipo preferido' }), { target: { value: 'infantil-id' } })
    fireEvent.click(screen.getByRole('button', { name: 'Guardar cambios' }))

    await waitFor(() => expect(putJson).toHaveBeenCalledWith('/api/v1/profile/', {
      firstName: 'Ana María', lastName: 'Pruebas', email: 'nueva@example.es', preferredTeamId: 'infantil-id',
    }))
    expect(onProfileUpdated).toHaveBeenCalledWith(expect.objectContaining({ displayName: 'Ana María Pruebas', email: 'nueva@example.es' }))
    expect(await screen.findByRole('status')).toHaveTextContent('Tu perfil se ha guardado correctamente.')
  })

  it('sube un avatar admitido y actualiza la sesión', async () => {
    vi.stubGlobal('fetch', vi.fn().mockResolvedValue(new Response(JSON.stringify(profile), { status: 200 })))
    const updated = { ...profile, avatarUrl: '/api/v1/profile/avatar/user-id' }
    vi.mocked(postForm).mockResolvedValueOnce(updated)
    const onProfileUpdated = vi.fn()
    render(<ProfilePage theme="light" onToggleTheme={() => undefined} user={user} onProfileUpdated={onProfileUpdated}/> )

    await screen.findByRole('combobox', { name: 'Equipo preferido' })
    const file = new File(['jpeg-data'], 'avatar.jpg', { type: 'image/jpeg' })
    fireEvent.change(screen.getByLabelText('Elegir foto de perfil'), { target: { files: [file] } })

    await waitFor(() => expect(postForm).toHaveBeenCalledWith('/api/v1/profile/avatar', expect.any(FormData)))
    expect(onProfileUpdated).toHaveBeenCalledWith(expect.objectContaining({ avatarUrl: updated.avatarUrl }))
  })
})
