import { useEffect, useState } from 'react'
import LandingPage from './features/landing/LandingPage'
import PublicFeedPage from './features/feed/PublicFeedPage'
import { type Theme } from './features/system/ThemeToggle'
import AuthPage from './features/auth/AuthPage'
import { type SessionUser } from './features/system/SiteHeader'
import ProfilePage from './features/profile/ProfilePage'
import AdministrationPage from './features/administration/AdministrationPage'
import TeamPage from './features/teams/TeamPage'
import './App.css'
import './features/system/site-header.css'
import './features/profile/profile.css'
import './features/administration/administration.css'
import './features/teams/teams.css'
import './brand-assets.css'

export type { Theme } from './features/system/ThemeToggle'

function readStoredTheme(): Theme {
  try {
    return localStorage.getItem('cb-bunol-theme') === 'dark' ? 'dark' : 'light'
  } catch {
    return 'light'
  }
}

function App() {
  const [theme, setTheme] = useState<Theme>(readStoredTheme)
  const [user, setUser] = useState<SessionUser | null>(null)
  const [sessionReady, setSessionReady] = useState(false)
  const path = window.location.pathname.replace(/\/$/, '') || '/'

  useEffect(() => {
    document.documentElement.dataset.theme = theme
    document.querySelector('meta[name="theme-color"]')?.setAttribute('content', theme === 'dark' ? '#051424' : '#f8fafc')
    try {
      localStorage.setItem('cb-bunol-theme', theme)
    } catch {
      // La aplicación sigue funcionando aunque el navegador bloquee el almacenamiento local.
    }
  }, [theme])

  useEffect(() => {
    let active = true
    void fetch('/api/v1/auth/me', { credentials: 'include', cache: 'no-store' })
      .then(async (response) => {
        if (response.ok && active) setUser(await response.json() as SessionUser)
      })
      .catch(() => undefined)
      .finally(() => { if (active) setSessionReady(true) })
    return () => { active = false }
  }, [])

  const toggleTheme = () => setTheme((current) => current === 'light' ? 'dark' : 'light')

  useEffect(() => {
    if (!sessionReady) return
    if (user && (path === '/' || path === '/auth')) window.location.replace('/feed')
    if (!user && path.startsWith('/admin')) window.location.replace('/auth?tab=login')
    if (!user && path === '/perfil') window.location.replace('/auth?tab=login')
    if (!user && path === '/equipo') window.location.replace('/auth?tab=login')
    if (user && path.startsWith('/admin') && !user.roles.includes('Administrador')) window.location.replace('/feed')
  }, [path, sessionReady, user])

  if (!sessionReady) return <div className="app-loading" role="status">Cargando…</div>
  if (user && (path === '/' || path === '/auth')) return <div className="app-loading" role="status">Abriendo tu feed…</div>
  if (!user && path.startsWith('/admin')) return <div className="app-loading" role="status">Abriendo acceso…</div>
  if (!user && path === '/perfil') return <div className="app-loading" role="status">Abriendo acceso…</div>
  if (!user && path === '/equipo') return <div className="app-loading" role="status">Abriendo acceso…</div>
  if (!user && path.startsWith('/admin')) return <div className="app-loading" role="status">Abriendo acceso…</div>
  if (user && path.startsWith('/admin') && !user.roles.includes('Administrador')) return <div className="app-loading" role="status">Volviendo al feed…</div>

  const onAuthenticated = (authenticatedUser: SessionUser) => {
    setUser(authenticatedUser)
    window.location.assign('/feed')
  }
  if (path.startsWith('/admin')) return <AdministrationPage theme={theme} onToggleTheme={toggleTheme} user={user!} initialTab={path === '/admin/solicitudes' ? 'requests' : undefined}/>
  if (path === '/perfil') return <ProfilePage theme={theme} onToggleTheme={toggleTheme} user={user!} onProfileUpdated={setUser}/>
  if (path === '/equipo') return <TeamPage theme={theme} onToggleTheme={toggleTheme} user={user!}/>
  if (path === '/auth') return <AuthPage theme={theme} onToggleTheme={toggleTheme} onAuthenticated={onAuthenticated}/>
  if (path === '/feed') return <PublicFeedPage theme={theme} onToggleTheme={toggleTheme} user={user}/>
  return <LandingPage theme={theme} onToggleTheme={toggleTheme} user={user}/>
}

export default App
