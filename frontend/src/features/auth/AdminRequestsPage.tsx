import { useCallback, useEffect, useState } from 'react'
import SiteHeader, { type SessionUser } from '../system/SiteHeader'
import { type Theme } from '../system/ThemeToggle'
import { postJson } from './authApi'
import './auth.css'

type RoleRequest = { id: string; displayName: string; email: string; requestedRole: string; status: string; createdAt: string }

export default function AdminRequestsPage({ theme, onToggleTheme, user }: {
  theme: Theme
  onToggleTheme: () => void
  user: SessionUser
}) {
  const [requests, setRequests] = useState<RoleRequest[]>([])
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState('')

  const refresh = useCallback(async () => {
    setError('')
    try {
      const response = await fetch('/api/v1/admin/role-requests/', { credentials: 'include', cache: 'no-store' })
      if (!response.ok) throw new Error(response.status === 403 ? 'Tu cuenta no tiene permisos de administración.' : 'No se pudieron cargar las solicitudes.')
      setRequests(await response.json() as RoleRequest[])
    } catch (requestError) {
      setError(requestError instanceof Error ? requestError.message : 'Error al cargar solicitudes.')
    } finally { setLoading(false) }
  }, [])

  useEffect(() => {
    const timer = window.setTimeout(() => { void refresh() }, 0)
    return () => window.clearTimeout(timer)
  }, [refresh])

  const resolve = async (id: string, decision: 'approve' | 'reject') => {
    setError('')
    try {
      await postJson(`/api/v1/admin/role-requests/${id}/${decision}`)
      await refresh()
    } catch (requestError) {
      setError(requestError instanceof Error ? requestError.message : 'No se pudo resolver la solicitud.')
    }
  }

  const pending = requests.filter((request) => request.status === 'Pending')

  return <main className="admin-page">
    <SiteHeader theme={theme} onToggleTheme={onToggleTheme} user={user}/>
    <section className="admin-content">
      <div className="admin-page-heading"><div><span className="section-kicker">ADMINISTRACIÓN DEL CLUB</span><h1>Solicitudes de rol</h1><p>Revisa las solicitudes para Jugador y Cuerpo Técnico.</p></div><span className="request-count">{pending.length} pendientes</span></div>
      {error && <div className="auth-alert error-alert" role="alert">{error}</div>}
      {loading ? <p className="auth-muted">Cargando solicitudes…</p> : requests.length === 0 ? <p className="auth-muted">No hay solicitudes por revisar.</p> : <div className="request-list">{requests.map((request) => <article className="request-item" key={request.id}><div className="request-user"><span className="request-avatar">{request.displayName.slice(0, 1).toUpperCase()}</span><div><strong>{request.displayName}</strong><small>{request.email}</small></div></div><span className="request-role">{request.requestedRole === 'CuerpoTecnico' ? 'Cuerpo Técnico' : request.requestedRole}</span><span className={`request-status ${request.status.toLowerCase()}`}>{request.status === 'Pending' ? 'Pendiente' : request.status === 'Approved' ? 'Aprobada' : 'Rechazada'}</span>{request.status === 'Pending' && <div className="request-actions"><button onClick={() => void resolve(request.id, 'approve')}>Aprobar</button><button onClick={() => void resolve(request.id, 'reject')}>Rechazar</button></div>}</article>)}</div>}
    </section>
  </main>
}
