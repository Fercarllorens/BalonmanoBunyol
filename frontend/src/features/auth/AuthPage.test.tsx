import { fireEvent, render, screen, waitFor } from '@testing-library/react'
import { describe, expect, it, vi } from 'vitest'
import AuthPage from './AuthPage'

vi.mock('./authApi', () => ({
  postJson: vi.fn(),
}))

import { postJson } from './authApi'

describe('AuthPage', () => {
  it('muestra el escudo del club en la marca del acceso', () => {
    window.history.replaceState({}, '', '/auth')
    render(<AuthPage theme="light" onToggleTheme={() => undefined} onAuthenticated={() => undefined}/> )

    expect(document.querySelector('.auth-brand-shield')).toHaveAttribute('src', '/club-bunol-escudo.jpeg')
    expect(document.querySelector('.auth-card-brand img')).toHaveAttribute('src', '/club-bunol-escudo.jpeg')
  })

  it('muestra login por defecto, alterna a registro y permite seleccionar el tipo de cuenta', () => {
    window.history.replaceState({}, '', '/auth')
    render(<AuthPage theme="light" onToggleTheme={() => undefined} onAuthenticated={() => undefined}/>)

    expect(screen.getByRole('heading', { name: 'Accede al portal del club' })).toBeInTheDocument()
    fireEvent.click(screen.getByRole('tab', { name: 'Crear cuenta' }))
    expect(screen.getByRole('heading', { name: 'Crea tu cuenta' })).toBeInTheDocument()
    fireEvent.click(screen.getByRole('button', { name: /Cuerpo Técnico/ }))
    expect(screen.getByText(/Administración revisará tu solicitud de rol/)).toBeInTheDocument()
  })

  it('envía login, comunica fallo y permite mostrar/ocultar contraseña', async () => {
    window.history.replaceState({}, '', '/auth?tab=login')
    vi.mocked(postJson).mockRejectedValueOnce(new Error('Credenciales no válidas'))
    render(<AuthPage theme="light" onToggleTheme={() => undefined} onAuthenticated={() => undefined}/>)

    const password = screen.getByPlaceholderText('Introduce tu contraseña') as HTMLInputElement
    expect(password.type).toBe('password')
    fireEvent.click(screen.getByRole('button', { name: 'Mostrar contraseña' }))
    expect(password.type).toBe('text')
    fireEvent.change(screen.getByPlaceholderText('nombre@correo.com'), { target: { value: 'ana@example.es' } })
    fireEvent.change(password, { target: { value: 'ContraseñaSegura1!' } })
    fireEvent.click(screen.getByRole('button', { name: /Acceder al portal del club/ }))

    await waitFor(() => expect(postJson).toHaveBeenCalledWith('/api/v1/auth/login', {
      email: 'ana@example.es', password: 'ContraseñaSegura1!', rememberMe: true,
    }))
    expect(await screen.findByRole('alert')).toHaveTextContent('Credenciales no válidas')
  })
})
