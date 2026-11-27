import { fireEvent, render, screen } from '@testing-library/react'
import { describe, expect, it, vi } from 'vitest'
import SiteHeader, { type SessionUser } from './SiteHeader'

vi.mock('../auth/authApi', () => ({ postJson: vi.fn() }))

const user: SessionUser = { id: 'user-id', displayName: 'Ana Jugadora', email: 'ana@example.es', roles: ['Jugador'] }

describe('SiteHeader', () => {
  it('abre el menú de usuario y ofrece personalizar el perfil y cerrar sesión', () => {
    render(<SiteHeader theme="light" onToggleTheme={() => undefined} user={user}/> )
    fireEvent.click(screen.getByRole('button', { name: 'Menú de usuario: Ana Jugadora' }))

    expect(screen.getByRole('menu')).toBeInTheDocument()
    expect(screen.getByRole('menuitem', { name: /Personalizar perfil/ })).toHaveAttribute('href', '/perfil')
    expect(screen.getByRole('menuitem', { name: /Cerrar sesión/ })).toBeInTheDocument()
    expect(screen.getByRole('menuitem', { name: /Personalizar perfil/ })).toHaveFocus()
  })

  it('cierra el menú al pulsar Escape', () => {
    render(<SiteHeader theme="light" onToggleTheme={() => undefined} user={user}/> )
    const trigger = screen.getByRole('button', { name: 'Menú de usuario: Ana Jugadora' })
    fireEvent.click(trigger)
    fireEvent.keyDown(document, { key: 'Escape' })
    expect(screen.queryByRole('menu')).not.toBeInTheDocument()
    expect(trigger).toHaveFocus()
  })

  it('permite abrir con flecha abajo y recorrer opciones con las flechas', () => {
    render(<SiteHeader theme="light" onToggleTheme={() => undefined} user={user}/> )
    const trigger = screen.getByRole('button', { name: 'Menú de usuario: Ana Jugadora' })
    fireEvent.keyDown(trigger, { key: 'ArrowDown' })
    const profileItem = screen.getByRole('menuitem', { name: /Personalizar perfil/ })
    const logoutItem = screen.getByRole('menuitem', { name: /Cerrar sesión/ })
    expect(profileItem).toHaveFocus()
    fireEvent.keyDown(profileItem, { key: 'ArrowDown' })
    expect(logoutItem).toHaveFocus()
    fireEvent.keyDown(logoutItem, { key: 'ArrowDown' })
    expect(profileItem).toHaveFocus()
  })

  it('cierra el menú al pulsar fuera', () => {
    render(<SiteHeader theme="light" onToggleTheme={() => undefined} user={user}/> )
    fireEvent.click(screen.getByRole('button', { name: 'Menú de usuario: Ana Jugadora' }))
    fireEvent.pointerDown(document.body)
    expect(screen.queryByRole('menu')).not.toBeInTheDocument()
  })

  it('muestra el acceso al panel solo a personas administradoras', () => {
    const { rerender } = render(<SiteHeader theme="light" onToggleTheme={() => undefined} user={user}/> )
    expect(screen.queryByRole('link', { name: 'Administración' })).not.toBeInTheDocument()
    rerender(<SiteHeader theme="light" onToggleTheme={() => undefined} user={{ ...user, roles: ['Administrador'] }}/>)
    expect(screen.getByRole('link', { name: 'Administración' })).toHaveAttribute('href', '/admin')
  })

  it('mantiene navegación y controles de cuenta en regiones independientes', () => {
    render(<SiteHeader theme="light" onToggleTheme={() => undefined} user={user}/> )
    const navigation = screen.getByRole('navigation', { name: 'Navegación de cuenta' })
    const accountMenu = screen.getByRole('button', { name: 'Menú de usuario: Ana Jugadora' })
    const themeToggle = screen.getByRole('button', { name: 'Cambiar a tema oscuro' })

    expect(navigation).toContainElement(screen.getByRole('link', { name: 'Feed' }))
    expect(navigation).toContainElement(screen.getByRole('link', { name: 'Equipo' }))
    expect(navigation).not.toContainElement(accountMenu)
    expect(navigation).not.toContainElement(themeToggle)
    expect(accountMenu.closest('.header-controls')).toContainElement(themeToggle)
  })
})
