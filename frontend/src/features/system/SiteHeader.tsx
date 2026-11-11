import { useState } from 'react'
import { postJson } from '../auth/authApi'
import { ThemeToggle, type Theme } from './ThemeToggle'

export type SessionUser = { id: string; email: string; displayName: string; roles: string[] }
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
          {isAdmin && <a href="/admin/solicitudes">Solicitudes</a>}
          <span className="global-user-name">{user.displayName}</span>
          <button className="global-logout" type="button" onClick={() => void logout()}>Cerrar sesión</button>
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
