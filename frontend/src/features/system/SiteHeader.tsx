import { useEffect, useRef, useState, type KeyboardEvent as ReactKeyboardEvent } from 'react'
import { postJson } from '../auth/authApi'
import { ThemeToggle, type Theme } from './ThemeToggle'

export type SessionUser = { id: string; email: string; displayName: string; roles: string[]; firstName?: string; lastName?: string; preferredTeamId?: string | null; avatarUrl?: string | null }
export type HeaderVariant = 'landing' | 'standard'

type SiteHeaderProps = {
  theme: Theme
  onToggleTheme: () => void
  user: SessionUser | null
  variant?: HeaderVariant
}

export default function SiteHeader({ theme, onToggleTheme, user, variant = 'standard' }: SiteHeaderProps) {
  const isAdmin = user?.roles.includes('Administrador') ?? false
  const brandHref = user ? '/feed' : '/'
  const [logoutError, setLogoutError] = useState('')
  const [accountMenuOpen, setAccountMenuOpen] = useState(false)
  const accountMenuRef = useRef<HTMLDivElement>(null)
  const accountMenuTriggerRef = useRef<HTMLButtonElement>(null)

  const focusMenuItem = (position: 'first' | 'last' | 'next' | 'previous') => {
    const items = accountMenuRef.current?.querySelectorAll<HTMLElement>('[role="menuitem"]')
    if (!items?.length) return
    const currentIndex = Array.from(items).indexOf(document.activeElement as HTMLElement)
    const index = position === 'first' ? 0
      : position === 'last' ? items.length - 1
        : position === 'next' ? (currentIndex + 1 + items.length) % items.length
          : (currentIndex - 1 + items.length) % items.length
    items[index]?.focus()
  }

  useEffect(() => {
    if (!accountMenuOpen) return
    focusMenuItem('first')
    const closeOnOutsideClick = (event: MouseEvent) => {
      if (event.target instanceof Node && !accountMenuRef.current?.contains(event.target)) setAccountMenuOpen(false)
    }
    const closeOnEscape = (event: KeyboardEvent) => {
      if (event.key === 'Escape') {
        setAccountMenuOpen(false)
        accountMenuTriggerRef.current?.focus()
      }
    }
    document.addEventListener('pointerdown', closeOnOutsideClick)
    document.addEventListener('keydown', closeOnEscape)
    return () => {
      document.removeEventListener('pointerdown', closeOnOutsideClick)
      document.removeEventListener('keydown', closeOnEscape)
    }
  }, [accountMenuOpen])

  const handleMenuKeyDown = (event: ReactKeyboardEvent<HTMLDivElement>) => {
    if (event.key === 'ArrowDown') {
      event.preventDefault()
      focusMenuItem('next')
    } else if (event.key === 'ArrowUp') {
      event.preventDefault()
      focusMenuItem('previous')
    } else if (event.key === 'Home') {
      event.preventDefault()
      focusMenuItem('first')
    } else if (event.key === 'End') {
      event.preventDefault()
      focusMenuItem('last')
    } else if (event.key === 'Tab') {
      setAccountMenuOpen(false)
    }
  }

  const toggleAccountMenu = () => setAccountMenuOpen((open) => !open)

  const handleAccountTriggerKeyDown = (event: ReactKeyboardEvent<HTMLButtonElement>) => {
    if (event.key === 'ArrowDown') {
      event.preventDefault()
      if (!accountMenuOpen) setAccountMenuOpen(true)
      else focusMenuItem('first')
    }
  }

  const logout = async () => {
    setLogoutError('')
    try {
      await postJson('/api/v1/auth/logout')
      window.location.assign('/')
    } catch {
      setLogoutError('No se pudo cerrar la sesión. Inténtalo de nuevo.')
    }
  }

  return <header className={`global-header ${variant === 'landing' ? 'site-header' : 'feed-header'}`}>
    <div className={variant === 'landing' ? 'site-header-inner' : 'global-header-inner'}>
      <a className="brand" href={brandHref} aria-label="Club Balonmano Buñol">
        <span className="brand-shield"><span className="material-symbols-outlined" aria-hidden="true">sports_handball</span></span>
        <span className="brand-name"><strong>C.BM. BUÑOL</strong><small>CLUB OFICIAL</small></span>
      </a>
      {variant === 'landing' && !user && <nav className="main-nav" aria-label="Navegación principal"><a href="#historia">El Club</a><a href="#categorias">Cantera</a><a href="#galeria">Galería</a></nav>}
      <nav className="global-nav" aria-label="Navegación de cuenta">
        {user ? <>
          <a href="/feed">Feed</a>
          <a href="/equipo">Equipo</a>
          {isAdmin && <a href="/admin">Administración</a>}
          <div className={`account-menu ${accountMenuOpen ? 'is-open' : ''}`} ref={accountMenuRef}>
            <button ref={accountMenuTriggerRef} className="account-menu-trigger" type="button" aria-label={`Menú de usuario: ${user.displayName}`} aria-haspopup="menu" aria-expanded={accountMenuOpen} aria-controls="account-menu-popover" onClick={toggleAccountMenu} onKeyDown={handleAccountTriggerKeyDown}>
              {user.avatarUrl ? <img className="account-menu-avatar" src={user.avatarUrl} alt=""/> : <span className="account-menu-avatar account-menu-initials" aria-hidden="true">{user.displayName.trim().split(/\s+/).slice(0, 2).map((part) => part[0]).join('').toUpperCase()}</span>}
              <span className="account-menu-label"><strong>{user.displayName}</strong><small>Mi cuenta</small></span>
              <span className="material-symbols-outlined account-menu-chevron" aria-hidden="true">expand_more</span>
            </button>
            {accountMenuOpen && <div id="account-menu-popover" className="account-menu-popover" role="menu" aria-label="Opciones de usuario" onKeyDown={handleMenuKeyDown}>
              <div className="account-menu-summary">
                <span className="account-menu-summary-avatar">{user.avatarUrl ? <img src={user.avatarUrl} alt=""/> : <span aria-hidden="true">{user.displayName.trim().split(/\s+/).slice(0, 2).map((part) => part[0]).join('').toUpperCase()}</span>}</span>
                <span className="account-menu-summary-text"><strong>{user.displayName}</strong><small>{user.email}</small></span>
              </div>
              <span className="account-menu-section-label">CUENTA</span>
              <a className="account-menu-item" href="/perfil" role="menuitem" onClick={() => setAccountMenuOpen(false)}><span className="account-menu-item-icon material-symbols-outlined" aria-hidden="true">manage_accounts</span><span><strong>Personalizar perfil</strong><small>Datos, foto y equipo preferido</small></span><span className="account-menu-item-arrow material-symbols-outlined" aria-hidden="true">chevron_right</span></a>
              <div className="account-menu-divider"/>
              <button className="account-menu-item account-menu-logout" type="button" role="menuitem" onClick={() => { setAccountMenuOpen(false); void logout() }}><span className="account-menu-item-icon material-symbols-outlined" aria-hidden="true">logout</span><span><strong>Cerrar sesión</strong><small>Salir de tu cuenta</small></span></button>
            </div>}
          </div>
        </> : <>
          <a href="/feed">Feed Social</a>
          <a className="global-login" href="/auth?tab=login">Iniciar sesión</a>
          <a className="global-register" href="/auth?tab=register">Registrarse</a>
        </>}
        <ThemeToggle theme={theme} onToggle={onToggleTheme}/>
      </nav>
      {logoutError && <span className="global-logout-error" role="alert">{logoutError}</span>}
    </div>
  </header>
}
