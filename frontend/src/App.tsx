import { useEffect, useState } from 'react'
import LandingPage from './features/landing/LandingPage'
import PublicFeedPage from './features/feed/PublicFeedPage'
import { type Theme } from './features/system/ThemeToggle'
import AuthPage from './features/auth/AuthPage'
import { type SessionUser } from './features/system/SiteHeader'
import AdminRequestsPage from './features/auth/AdminRequestsPage'
import './App.css'
import './features/system/site-header.css'

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
    if (!user && path === '/admin/solicitudes') window.location.replace('/auth?tab=login')
    if (user && path === '/admin/solicitudes' && !user.roles.includes('Administrador')) window.location.replace('/feed')
  }, [path, sessionReady, user])

  if (!sessionReady) return <div className="app-loading" role="status">Cargando…</div>
  if (user && (path === '/' || path === '/auth')) return <div className="app-loading" role="status">Abriendo tu feed…</div>
  if (!user && path === '/admin/solicitudes') return <div className="app-loading" role="status">Abriendo acceso…</div>
  if (user && path === '/admin/solicitudes' && !user.roles.includes('Administrador')) return <div className="app-loading" role="status">Volviendo al feed…</div>

  const onAuthenticated = (authenticatedUser: SessionUser) => {
    setUser(authenticatedUser)
    window.location.assign('/feed')
  }
  if (path === '/admin/solicitudes') return <AdminRequestsPage theme={theme} onToggleTheme={toggleTheme} user={user!}/>
  if (path === '/auth') return <AuthPage theme={theme} onToggleTheme={toggleTheme} onAuthenticated={onAuthenticated}/>
  if (path === '/feed') return <PublicFeedPage theme={theme} onToggleTheme={toggleTheme} user={user}/>
  return <LandingPage theme={theme} onToggleTheme={toggleTheme} user={user}/>
}

export default App
