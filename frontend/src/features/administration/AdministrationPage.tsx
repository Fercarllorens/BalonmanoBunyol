import { type FormEvent, useCallback, useEffect, useMemo, useState } from 'react'
import SiteHeader, { type SessionUser } from '../system/SiteHeader'
import { type Theme } from '../system/ThemeToggle'
import { deleteRequest, postJson, putJson } from '../auth/authApi'

type Tab = 'requests' | 'coaches' | 'users' | 'teams'
type RoleRequest = { id: string; userId: string; displayName: string; email: string; requestedRole: string; status: string; createdAt: string }
type Team = { id: string; name: string; category: string; isActive: boolean }
type AdminUser = { id: string; displayName: string; email: string; roles: string[]; teams: Team[] }
type PlayerMembership = { userId: string; displayName: string; email: string; jerseyNumber: number | null; position: string }
const roleOptions = [
  { value: 'SocioAficion', label: 'Socio / Afición' },
  { value: 'Jugador', label: 'Jugador/a' },
  { value: 'CuerpoTecnico', label: 'Cuerpo Técnico' },
  { value: 'Administrador', label: 'Administrador' },
]
const categories = ['Benjamín', 'Alevín', 'Infantil', 'Cadete', 'Juvenil', 'Senior']

function TeamAssignmentPicker({ teams, selectedTeamIds, targetName, saving, expanded, onToggle, onChange }: {
  teams: Team[]
  selectedTeamIds: string[]
  targetName: string
  saving: boolean
  expanded: boolean
  onToggle: () => void
  onChange: (teamIds: string[]) => void
}) {
  const [teamQuery, setTeamQuery] = useState('')
  const normalizedQuery = teamQuery.trim().toLocaleLowerCase('es')
  const selectableTeams = teams.filter((team) => team.isActive || selectedTeamIds.includes(team.id))
  const filteredTeams = selectableTeams.filter((team) =>
    `${team.name} ${team.category}`.toLocaleLowerCase('es').includes(normalizedQuery))
  const categoriesInOrder = [...new Set(filteredTeams.map((team) => team.category))]
  const selectedTeams = teams.filter((team) => selectedTeamIds.includes(team.id))

  const toggleTeam = (teamId: string) => {
    onChange(selectedTeamIds.includes(teamId)
      ? selectedTeamIds.filter((id) => id !== teamId)
      : [...selectedTeamIds, teamId])
  }

  return <div className={`admin-team-assignment${expanded ? ' expanded' : ''}`}>
    <button className="admin-team-assignment-trigger" type="button" aria-expanded={expanded} aria-label={`Equipos de ${targetName}: ${selectedTeams.length} asignados`} onClick={onToggle}>
      <span className="material-symbols-outlined" aria-hidden="true">groups</span>
      <span className="admin-team-assignment-trigger-copy"><strong>{selectedTeams.length} {selectedTeams.length === 1 ? 'equipo asignado' : 'equipos asignados'}</strong><small>{selectedTeams.length ? selectedTeams.map((team) => team.name).join(' · ') : 'Sin equipos asignados'}</small></span>
      <span className="material-symbols-outlined admin-team-assignment-chevron" aria-hidden="true">expand_more</span>
    </button>
    {expanded && <div className="admin-team-picker" aria-label={`Equipos asignados a ${targetName}`}>
      <div className="admin-team-picker-heading">
        <div><strong>Selecciona los equipos</strong><span>{selectedTeams.length} seleccionados</span></div>
        <label className="admin-team-picker-search"><span className="material-symbols-outlined" aria-hidden="true">search</span><input aria-label={`Buscar equipos de ${targetName}`} value={teamQuery} onChange={(event) => setTeamQuery(event.target.value)} placeholder="Buscar equipo o categoría…"/></label>
      </div>
      {selectedTeams.length === 0 && <p className="admin-team-picker-empty-summary">No hay equipos asignados todavía.</p>}
      <div className="admin-team-options" aria-label={`Equipos disponibles para ${targetName}`}>
        {categoriesInOrder.length === 0 ? <p className="admin-empty">No hay equipos que coincidan con la búsqueda.</p> : categoriesInOrder.map((category) => <section className="admin-team-category-group" aria-label={`Categoría ${category}`} key={category}>
          <h4>{category}</h4>
          <div className="admin-team-option-list">{filteredTeams.filter((team) => team.category === category).map((team) => {
            const checked = selectedTeamIds.includes(team.id)
            return <label className={`admin-team-option${checked ? ' selected' : ''}${team.isActive ? '' : ' inactive'}`} key={team.id}>
              <input type="checkbox" aria-label={`Asignar ${team.name} a ${targetName}`} checked={checked} disabled={saving || (!team.isActive && !checked)} onChange={() => toggleTeam(team.id)}/>
              <span className="admin-team-option-check material-symbols-outlined" aria-hidden="true">{checked ? 'check_box' : 'check_box_outline_blank'}</span>
              <span className="admin-team-option-copy"><strong>{team.name}</strong></span>
              {!team.isActive && <span className="admin-team-inactive-label">Inactivo</span>}
              {checked && <span className="admin-team-option-status">Asignado</span>}
            </label>
          })}</div>
        </section>)}
      </div>
    </div>}
  </div>
}

export default function AdministrationPage({ theme, onToggleTheme, user, initialTab = 'requests' }: {
  theme: Theme
  onToggleTheme: () => void
  user: SessionUser
  initialTab?: Tab
}) {
  const [tab, setTab] = useState<Tab>(initialTab)
  const [queryTab, setQueryTab] = useState<Tab>(initialTab)
  const [requests, setRequests] = useState<RoleRequest[]>([])
  const [teams, setTeams] = useState<Team[]>([])
  const [rosterByTeam, setRosterByTeam] = useState<Record<string, PlayerMembership[]>>({})
  const [users, setUsers] = useState<AdminUser[]>([])
  const [query, setQuery] = useState('')
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState('')
  const [notice, setNotice] = useState('')
  const [saving, setSaving] = useState(false)
  const [savingTeamsForUserId, setSavingTeamsForUserId] = useState<string | null>(null)
  const [selectedTeams, setSelectedTeams] = useState<Record<string, string[]>>({})
  const [expandedCoachId, setExpandedCoachId] = useState<string | null>(null)
  const [editingTeamId, setEditingTeamId] = useState<string | null>(null)
  const [rosterUserId, setRosterUserId] = useState('')
  const [rosterTeamId, setRosterTeamId] = useState('')
  const [rosterDorsal, setRosterDorsal] = useState('')
  const [rosterPosition, setRosterPosition] = useState('Universal')
  const [teamName, setTeamName] = useState('')
  const [teamCategory, setTeamCategory] = useState('Senior')

  const loadRequests = useCallback(async () => {
    const response = await fetch('/api/v1/admin/role-requests/?pendingOnly=true', { credentials: 'include', cache: 'no-store' })
    if (!response.ok) throw new Error(response.status === 403 ? 'Tu cuenta no tiene permisos de administración.' : 'No se pudieron cargar las solicitudes.')
    setRequests(await response.json() as RoleRequest[])
  }, [])
  const loadTeams = useCallback(async () => {
    const response = await fetch('/api/v1/admin/teams', { credentials: 'include', cache: 'no-store' })
    if (!response.ok) throw new Error('No se pudieron cargar los equipos.')
    setTeams(await response.json() as Team[])
  }, [])
  const searchUsers = useCallback(async (term: string, coachesOnly = false) => {
    if (!coachesOnly && term.trim().length < 2) { setUsers([]); return [] as AdminUser[] }
    const queryParams = new URLSearchParams()
    if (term.trim()) queryParams.set('query', term.trim())
    if (coachesOnly) queryParams.set('coachesOnly', 'true')
    const response = await fetch(`/api/v1/admin/users?${queryParams.toString()}`, { credentials: 'include', cache: 'no-store' })
    if (!response.ok) throw new Error('No se pudieron buscar las cuentas.')
    const result = await response.json() as AdminUser[]
    setUsers(result)
    return result
  }, [])
  const searchPlayers = async (term: string) => {
    if (term.trim().length < 2) return [] as AdminUser[]
    const response = await fetch(`/api/v1/admin/users?query=${encodeURIComponent(term.trim())}&playersOnly=true`, { credentials: 'include', cache: 'no-store' })
    if (!response.ok) throw new Error('No se pudieron buscar jugadores.')
    const players = await response.json() as AdminUser[]
    setUsers(players)
    return players
  }
  useEffect(() => {
    if (!rosterTeamId) return
    const timer = window.setTimeout(() => { void searchPlayers(query).catch((searchError: unknown) => setError(searchError instanceof Error ? searchError.message : 'No se pudieron buscar jugadores.')) }, 220)
    return () => window.clearTimeout(timer)
  }, [query, rosterTeamId])
  const refresh = useCallback(async () => {
    setError('')
    try { await Promise.all([loadRequests(), loadTeams()]) }
    catch (loadError) { setError(loadError instanceof Error ? loadError.message : 'No se pudo cargar el panel.') }
    finally { setLoading(false) }
  }, [loadRequests, loadTeams])

  useEffect(() => {
    const timer = window.setTimeout(() => { void refresh() }, 0)
    return () => window.clearTimeout(timer)
  }, [refresh])
  useEffect(() => {
    if (!['users', 'coaches'].includes(queryTab)) return
    const coachesOnly = queryTab === 'coaches'
    const timer = window.setTimeout(() => {
      void searchUsers(query, coachesOnly).catch((searchError: unknown) => setError(searchError instanceof Error ? searchError.message : 'No se pudieron buscar las cuentas.'))
    }, coachesOnly && query.length === 0 ? 0 : 250)
    return () => window.clearTimeout(timer)
  }, [query, queryTab, searchUsers])

  const pendingRequests = useMemo(() => requests.filter((request) => request.status === 'Pending'), [requests])
  const coaches = useMemo(() => users.filter((item) => item.roles.includes('CuerpoTecnico') && !item.roles.includes('Administrador')), [users])

  const resolveRequest = async (id: string, decision: 'approve' | 'reject') => {
    setError(''); setNotice('')
    try {
      await postJson(`/api/v1/admin/role-requests/${id}/${decision}`)
      await loadRequests()
      setNotice(decision === 'approve' ? 'Solicitud aprobada y rol actualizado.' : 'Solicitud rechazada.')
    } catch (actionError) { setError(actionError instanceof Error ? actionError.message : 'No se pudo resolver la solicitud.') }
  }

  const changeRole = async (target: AdminUser, role: string) => {
    if (role === 'Administrador' && !window.confirm(`¿Quieres cambiar a ${target.displayName} al rol Administrador?`)) return
    setError(''); setNotice('')
    try {
      await putJson(`/api/v1/admin/users/${target.id}/role`, { role })
      const currentUsers = await searchUsers(query)
      if (queryTab === 'coaches') setUsers(currentUsers.filter((item) => item.roles.includes('CuerpoTecnico') || item.roles.includes('Administrador')))
      setNotice(`Rol de ${target.displayName} actualizado.`)
    } catch (actionError) { setError(actionError instanceof Error ? actionError.message : 'No se pudo cambiar el rol.') }
  }

  const assignTeams = async (target: AdminUser) => {
    setSavingTeamsForUserId(target.id); setError(''); setNotice('')
    try {
      await putJson(`/api/v1/admin/users/${target.id}/teams`, { teamIds: selectedTeams[target.id] ?? target.teams.map((team) => team.id) })
      await searchUsers(query, true)
      setSelectedTeams((current) => { const next = { ...current }; delete next[target.id]; return next })
      setNotice(`Equipos de ${target.displayName} actualizados.`)
    } catch (actionError) { setError(actionError instanceof Error ? actionError.message : 'No se pudieron guardar las asignaciones.') }
    finally { setSavingTeamsForUserId(null) }
  }

  useEffect(() => {
    if (!expandedCoachId) return
    const closeOnOutsideClick = (event: PointerEvent) => {
      if (event.target instanceof Element && !event.target.closest('.admin-team-assignment')) setExpandedCoachId(null)
    }
    const closeOnEscape = (event: KeyboardEvent) => {
      if (event.key === 'Escape') setExpandedCoachId(null)
    }
    document.addEventListener('pointerdown', closeOnOutsideClick)
    document.addEventListener('keydown', closeOnEscape)
    return () => {
      document.removeEventListener('pointerdown', closeOnOutsideClick)
      document.removeEventListener('keydown', closeOnEscape)
    }
  }, [expandedCoachId])

  const createTeam = async (event: FormEvent<HTMLFormElement>) => {
    event.preventDefault(); setSaving(true); setError(''); setNotice('')
    try {
      if (editingTeamId) await putJson(`/api/v1/admin/teams/${editingTeamId}`, { name: teamName, category: teamCategory })
      else await postJson('/api/v1/admin/teams', { name: teamName, category: teamCategory })
      setTeamName('')
      setEditingTeamId(null)
      await loadTeams()
      setNotice(editingTeamId ? 'Equipo actualizado correctamente.' : 'Equipo creado correctamente.')
    } catch (actionError) { setError(actionError instanceof Error ? actionError.message : 'No se pudo crear el equipo.') }
    finally { setSaving(false) }
  }

  const toggleTeam = async (team: Team) => {
    setError(''); setNotice('')
    try {
      await putJson(`/api/v1/admin/teams/${team.id}/active?isActive=${!team.isActive}`, {})
      await loadTeams()
      setNotice(team.isActive ? 'Equipo desactivado.' : 'Equipo activado.')
    } catch (actionError) { setError(actionError instanceof Error ? actionError.message : 'No se pudo cambiar el equipo.') }
  }

  const loadTeamRoster = async (teamId: string) => {
    const response = await fetch(`/api/v1/admin/teams/${teamId}/players`, { credentials: 'include', cache: 'no-store' })
    if (!response.ok) throw new Error('No se pudo cargar la plantilla del equipo.')
    const roster = await response.json() as PlayerMembership[]
    setRosterByTeam((current) => ({ ...current, [teamId]: roster }))
  }

  const savePlayerRoster = async (event: FormEvent<HTMLFormElement>) => {
    event.preventDefault()
    if (!rosterUserId || !rosterTeamId) return
    setSaving(true); setError(''); setNotice('')
    try {
      await putJson(`/api/v1/admin/teams/${rosterTeamId}/players/${rosterUserId}`, { userId: rosterUserId, jerseyNumber: rosterDorsal ? Number(rosterDorsal) : null, position: rosterPosition })
      await loadTeamRoster(rosterTeamId)
      setRosterUserId(''); setRosterDorsal(''); setQuery('')
      setNotice('Jugador asignado a la plantilla.')
    } catch (actionError) { setError(actionError instanceof Error ? actionError.message : 'No se pudo asignar el jugador.') }
    finally { setSaving(false) }
  }

  const removeRosterPlayer = async (teamId: string, player: PlayerMembership) => {
    if (!window.confirm(`¿Quitar a ${player.displayName} de la plantilla?`)) return
    try {
      await deleteRequest(`/api/v1/admin/teams/${teamId}/players/${player.userId}`)
      await loadTeamRoster(teamId)
      setNotice(`${player.displayName} retirado de la plantilla.`)
    } catch (actionError) { setError(actionError instanceof Error ? actionError.message : 'No se pudo retirar al jugador.') }
  }

  const tabItems: { id: Tab; label: string; icon: string }[] = [
    { id: 'requests', label: `Solicitudes${pendingRequests.length ? ` · ${pendingRequests.length}` : ''}`, icon: 'pending_actions' },
    { id: 'coaches', label: 'Entrenadores y equipos', icon: 'groups' },
    { id: 'users', label: 'Usuarios y roles', icon: 'manage_accounts' },
    { id: 'teams', label: 'Equipos', icon: 'sports_handball' },
  ]

  return <main className="admin-page">
    <SiteHeader theme={theme} onToggleTheme={onToggleTheme} user={user}/>
    <section className="admin-content admin-hub">
      <header className="admin-hub-heading"><span className="admin-hub-icon material-symbols-outlined" aria-hidden="true">admin_panel_settings</span><div><span className="section-kicker">GESTIÓN DEL CLUB</span><h1>Panel de administración</h1><p>Gestiona solicitudes, personas y equipos del club.</p></div></header>
      <nav className="admin-tabs" aria-label="Secciones de administración">{tabItems.map((item) => <button key={item.id} type="button" className={tab === item.id ? 'selected' : ''} onClick={() => { setTab(item.id); if (item.id !== queryTab) { setQuery(''); setUsers([]); setSelectedTeams({}); setQueryTab(item.id) } setError(''); setNotice('') }}><span className="material-symbols-outlined" aria-hidden="true">{item.icon}</span>{item.label}</button>)}</nav>
      {error && <div className="admin-notice error" role="alert">{error}<button type="button" aria-label="Cerrar error" onClick={() => setError('')}>×</button></div>}
      {notice && <div className="admin-notice success" role="status">{notice}</div>}
      {loading ? <p className="admin-empty" role="status">Cargando panel…</p> : <>
        {tab === 'requests' && <section className="admin-section"><div className="admin-section-heading"><div><h2>Solicitudes pendientes</h2><p>Revisa las solicitudes de Jugador y Cuerpo Técnico.</p></div><span className="admin-count">{pendingRequests.length} pendientes</span></div>{pendingRequests.length === 0 ? <p className="admin-empty">No hay solicitudes por revisar.</p> : <div className="admin-table-wrap"><table className="admin-table"><thead><tr><th>Cuenta</th><th>Rol solicitado</th><th>Fecha</th><th>Acciones</th></tr></thead><tbody>{pendingRequests.map((request) => <tr key={request.id}><td><strong>{request.displayName}</strong><small>{request.email}</small></td><td>{request.requestedRole === 'CuerpoTecnico' ? 'Cuerpo Técnico' : 'Jugador/a'}</td><td>{new Date(request.createdAt).toLocaleDateString('es-ES')}</td><td className="admin-row-actions"><button type="button" className="admin-approve" onClick={() => void resolveRequest(request.id, 'approve')}>Aprobar</button><button type="button" className="admin-reject" onClick={() => void resolveRequest(request.id, 'reject')}>Rechazar</button></td></tr>)}</tbody></table></div>}</section>}
       {tab === 'coaches' && <section className="admin-section"><div className="admin-section-heading"><div><h2>Cuerpo técnico y equipos</h2><p>Asigna uno o varios equipos a las cuentas de Cuerpo Técnico. Administración puede acceder a todos sin asignación.</p></div></div><label className="admin-search"><span className="material-symbols-outlined" aria-hidden="true">search</span><input aria-label="Buscar entrenadores" value={query} onChange={(event) => setQuery(event.target.value)} placeholder="Buscar por nombre o correo…"/></label>{coaches.length === 0 ? <p className="admin-empty">{query.length < 2 ? 'No hay cuentas de Cuerpo Técnico asignables.' : 'No se encontraron cuentas de Cuerpo Técnico.'}</p> : <div className="admin-user-list">{coaches.map((target) => {
          const assignedIds = target.teams.map((team) => team.id)
          const selectedIds = selectedTeams[target.id] ?? assignedIds
          const hasChanges = target.id in selectedTeams && (selectedIds.length !== assignedIds.length || selectedIds.some((id) => !assignedIds.includes(id)))
          return <article className="admin-user-card admin-coach-card" key={target.id}>
            <div className="admin-user-identity"><span className="admin-user-avatar">{target.displayName.slice(0, 1).toUpperCase()}</span><div><strong>{target.displayName}</strong><small>{target.email}</small><span className="admin-role-label">{target.roles.includes('Administrador') ? 'Administrador' : 'Cuerpo Técnico'}</span></div></div>
            <div className="admin-coach-assignments">
              <TeamAssignmentPicker teams={teams} selectedTeamIds={selectedIds} targetName={target.displayName} saving={savingTeamsForUserId === target.id} expanded={expandedCoachId === target.id} onToggle={() => setExpandedCoachId((current) => current === target.id ? null : target.id)} onChange={(teamIds) => setSelectedTeams((current) => ({ ...current, [target.id]: teamIds }))}/>
              <div className="admin-team-save"><small>{hasChanges ? 'Tienes cambios de asignación pendientes.' : 'La asignación actual está guardada.'}</small><button type="button" className="admin-primary-button" disabled={savingTeamsForUserId === target.id || !hasChanges} onClick={() => void assignTeams(target)}>{savingTeamsForUserId === target.id ? 'Guardando…' : 'Guardar equipos'}</button></div>
            </div>
          </article>
        })}</div>}</section>}
        {tab === 'users' && <section className="admin-section"><div className="admin-section-heading"><div><h2>Usuarios y roles</h2><p>Busca una cuenta y establece su rol principal.</p></div></div><label className="admin-search"><span className="material-symbols-outlined" aria-hidden="true">search</span><input aria-label="Buscar usuarios" value={query} onChange={(event) => setQuery(event.target.value)} placeholder="Nombre o correo electrónico…"/></label>{query.trim().length < 2 ? <p className="admin-empty">Escribe al menos dos caracteres para buscar.</p> : users.length === 0 ? <p className="admin-empty">No se encontraron cuentas.</p> : <div className="admin-user-list">{users.map((target) => <article className="admin-user-card admin-role-card" key={target.id}><div className="admin-user-identity"><span className="admin-user-avatar">{target.displayName.slice(0, 1).toUpperCase()}</span><div><strong>{target.displayName}</strong><small>{target.email}</small><span className="admin-role-label">Actual: {target.roles.map((role) => roleOptions.find((option) => option.value === role)?.label ?? role).join(', ') || 'Sin rol'}</span></div></div><label className="admin-team-select"><span>Nuevo rol principal</span><select aria-label={`Rol de ${target.displayName}`} value={target.roles.find((role) => roleOptions.some((option) => option.value === role)) ?? ''} onChange={(event) => void changeRole(target, event.target.value)}>{roleOptions.map((role) => <option key={role.value} value={role.value}>{role.label}</option>)}</select></label></article>)}</div>}</section>}
        {tab === 'teams' && <section className="admin-section"><div className="admin-section-heading"><div><h2>Equipos y plantillas</h2><p>Crea equipos, administra su categoría y mueve las plantillas cuando sea necesario.</p></div></div><form className="admin-team-form" onSubmit={(event) => void createTeam(event)}><label><span>Nombre del equipo</span><input aria-label="Nombre del equipo" value={teamName} onChange={(event) => setTeamName(event.target.value)} maxLength={120} required placeholder="Ej. Senior A"/></label><label><span>Categoría</span><select aria-label="Categoría" value={teamCategory} onChange={(event) => setTeamCategory(event.target.value)}>{categories.map((category) => <option key={category}>{category}</option>)}</select></label><div className="admin-team-form-actions"><button className="admin-primary-button" type="submit" disabled={saving}>{saving ? 'Guardando…' : editingTeamId ? 'Guardar equipo' : 'Crear equipo'}</button>{editingTeamId && <button className="admin-cancel-button" type="button" onClick={() => { setEditingTeamId(null); setTeamName('') }}>Cancelar</button>}</div></form>{teams.length === 0 ? <p className="admin-empty">Todavía no hay equipos. Crea el primero con el formulario.</p> : <div className="admin-team-list">{teams.map((team) => <article className="admin-team-row admin-team-management" key={team.id}><span className="material-symbols-outlined" aria-hidden="true">sports_handball</span><div><strong>{team.name}</strong><small>Categoría {team.category}</small></div><span className={`admin-team-status ${team.isActive ? 'active' : ''}`}>{team.isActive ? 'Activo' : 'Inactivo'}</span><button type="button" onClick={() => { setEditingTeamId(team.id); setTeamName(team.name); setTeamCategory(team.category); window.scrollTo({ top: 0, behavior: 'smooth' }) }}>Editar</button><button type="button" onClick={() => void toggleTeam(team)}>{team.isActive ? 'Desactivar' : 'Activar'}</button><button type="button" aria-label={`Plantilla · ${team.name}`} onClick={() => { setRosterTeamId(team.id); void loadTeamRoster(team.id) }}>Plantilla</button></article>)}</div>}{rosterTeamId && <section className="admin-roster-panel"><div className="admin-section-heading"><div><h3>Plantilla · {teams.find((team) => team.id === rosterTeamId)?.name}</h3><p>Administración puede añadir o retirar jugadores del equipo.</p></div><button className="admin-cancel-button" type="button" onClick={() => setRosterTeamId('')}>Cerrar</button></div><form className="admin-team-form" onSubmit={(event) => void savePlayerRoster(event)}><label className="admin-roster-search"><span>Buscar jugador</span><input aria-label="Buscar jugador de plantilla" value={query} onChange={(event) => setQuery(event.target.value)} placeholder="Nombre o correo"/></label>{users.length > 0 && <label><span>Cuenta</span><select aria-label="Cuenta de jugador" value={rosterUserId} onChange={(event) => setRosterUserId(event.target.value)}><option value="">Seleccionar…</option>{users.map((candidate) => <option key={candidate.id} value={candidate.id}>{candidate.displayName} · {candidate.email} · {candidate.roles.includes('CuerpoTecnico') ? 'Cuerpo Técnico' : 'Jugador/a'}</option>)}</select></label>}<label><span>Dorsal</span><input type="number" min="0" max="99" aria-label="Dorsal del jugador" value={rosterDorsal} onChange={(event) => setRosterDorsal(event.target.value)}/></label><label><span>Posición</span><select aria-label="Posición del jugador" value={rosterPosition} onChange={(event) => setRosterPosition(event.target.value)}>{['Portero', 'Extremo Izquierdo', 'Lateral Izquierdo', 'Central', 'Pivote', 'Lateral Derecho', 'Extremo Derecho', 'Universal'].map((item) => <option key={item}>{item}</option>)}</select></label><button className="admin-primary-button" disabled={saving || !rosterUserId}>{saving ? 'Guardando…' : 'Añadir jugador'}</button></form>{(rosterByTeam[rosterTeamId] ?? []).length === 0 ? <p className="admin-empty">No hay jugadores en esta plantilla.</p> : <div className="admin-roster-list">{rosterByTeam[rosterTeamId].map((player) => <article key={player.userId}><strong>#{player.jerseyNumber ?? '—'} · {player.displayName}</strong><span>{player.position}</span><button type="button" onClick={() => void removeRosterPlayer(rosterTeamId, player)}>Quitar</button></article>)}</div>}</section>}</section>}
      </>}
    </section>
  </main>
}
