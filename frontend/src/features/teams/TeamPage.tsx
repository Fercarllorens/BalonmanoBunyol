import { type FormEvent, useCallback, useEffect, useMemo, useRef, useState } from 'react'
import SiteHeader, { type SessionUser } from '../system/SiteHeader'
import { type Theme } from '../system/ThemeToggle'
import { getCsrfToken } from '../auth/authApi'

type Team = { id: string; name: string; category: string; access: 'Administrador' | 'CuerpoTecnico' | 'Jugador' }
type TeamPlayer = { userId: string; displayName: string; email: string; avatarUrl: string | null; jerseyNumber: number | null; position: string; matchesPlayed: number; minutesPlayed: number; goals: number; saves: number }
type TeamCallup = { playerId: string; displayName: string; jerseyNumber: number | null; position: string; attendance: string }
type TeamMatch = { id: string; opponent: string; startsAt: string; venue: string; round: string; callups: TeamCallup[] }
type TeamEvent = { id: string; title: string; kind: string; startsAt: string; endsAt: string | null; venue: string; notes: string }
type CalendarItem = { id: string; source: 'Planificacion' | 'Evento' | 'Partido'; kind: string; title: string; startsAt: string; endsAt: string | null; venue: string; notes: string; isCancelled: boolean; trainingSlotId: string | null; trainingDate: string | null; canManage: boolean; callups: TeamCallup[] | null }
type TrainingSlot = { id: string; dayOfWeek: number; startsAt: string; endsAt: string; venue: string }
type TrainingPlan = { effectiveFrom: string; slots: TrainingSlot[] }
type TeamCalendar = { seasonStart: string; seasonEnd: string; month: string; items: CalendarItem[]; plans: TrainingPlan[] }
type BoardAttachment = { id: string; fileName: string; contentType: string; length: number; url: string }
type BoardPost = { id: string; authorId: string; authorName: string; content: string; createdAt: string; attachments: BoardAttachment[] }
type TeamSpace = { team: Team; players: TeamPlayer[]; matches: TeamMatch[]; events: TeamEvent[]; boardPosts: BoardPost[]; canManage: boolean }
type Candidate = { userId: string; displayName: string; email: string }
type Tab = 'plantilla' | 'pizarra' | 'calendario-v2' | 'estadisticas'

const positions = ['Portero', 'Extremo Izquierdo', 'Lateral Izquierdo', 'Central', 'Pivote', 'Lateral Derecho', 'Extremo Derecho', 'Universal']
const weekDays = ['Domingo', 'Lunes', 'Martes', 'Miércoles', 'Jueves', 'Viernes', 'Sábado']

function monthDateKey(date: Date) { return `${date.getFullYear()}-${String(date.getMonth() + 1).padStart(2, '0')}` }
function localDateKey(date: Date) { return `${date.getFullYear()}-${String(date.getMonth() + 1).padStart(2, '0')}-${String(date.getDate()).padStart(2, '0')}` }
function seasonStartKey(date = new Date()) { return `${date.getMonth() >= 6 ? date.getFullYear() : date.getFullYear() - 1}-07-01` }
function seasonEndKey(date = new Date()) { return `${date.getMonth() >= 6 ? date.getFullYear() + 1 : date.getFullYear()}-06-30` }
function monthLabel(value: string) { return new Date(`${value}-15T12:00:00`).toLocaleDateString('es-ES', { month: 'long', year: 'numeric' }) }
function monthDays(value: string) {
  const [year, month] = value.split('-').map(Number)
  const first = new Date(year, month - 1, 1)
  const offset = (first.getDay() + 6) % 7
  const days = new Date(year, month, 0).getDate()
  return [...Array(offset).fill(null), ...Array.from({ length: days }, (_, index) => new Date(year, month - 1, index + 1))]
}

function renderCalendar(calendar: TeamCalendar | null, calendarMonth: string, days: (Date | null)[], itemsByDay: Map<string, CalendarItem[]>, plan: TrainingPlan | undefined, canManage: boolean, busy: boolean, isFirstMonth: boolean, isLastMonth: boolean, eventFormOpen: boolean, matchFormOpen: boolean, trainingPlanOpen: boolean, editingEvent: CalendarItem | null, editingTraining: CalendarItem | null, players: TeamPlayer[], currentUserId: string, onPrevious: () => void, onNext: () => void, onToggleEventForm: () => void, onToggleMatchForm: () => void, onTogglePlanForm: () => void, onCreateEvent: (event: FormEvent<HTMLFormElement>) => void, onCreateMatch: (event: FormEvent<HTMLFormElement>) => void, onSavePlan: (event: FormEvent<HTMLFormElement>) => void, onEditEvent: (item: CalendarItem) => void, onCancelEvent: (item: CalendarItem) => void, onEditTraining: (item: CalendarItem) => void, onRestoreTraining: (item: CalendarItem) => void, onSaveEvent: (event: FormEvent<HTMLFormElement>) => void, onSaveTraining: (event: FormEvent<HTMLFormElement>) => void, onSetAttendance: (matchId: string, attendance: string) => void, onCloseEvent: () => void, onCloseTraining: () => void) {
  return <TeamCalendarView calendar={calendar} calendarMonth={calendarMonth} calendarDays={days} itemsByDay={itemsByDay} currentPlan={plan} canManage={canManage} busy={busy} isFirstMonth={isFirstMonth} isLastMonth={isLastMonth} eventFormOpen={eventFormOpen} matchFormOpen={matchFormOpen} trainingPlanOpen={trainingPlanOpen} editingEvent={editingEvent} editingTraining={editingTraining} players={players} currentUserId={currentUserId} onPrevious={onPrevious} onNext={onNext} onToggleEventForm={onToggleEventForm} onToggleMatchForm={onToggleMatchForm} onTogglePlanForm={onTogglePlanForm} onCreateEvent={onCreateEvent} onCreateMatch={onCreateMatch} onSavePlan={onSavePlan} onEditEvent={onEditEvent} onCancelEvent={onCancelEvent} onEditTraining={onEditTraining} onRestoreTraining={onRestoreTraining} onSaveEvent={onSaveEvent} onSaveTraining={onSaveTraining} onSetAttendance={onSetAttendance} onCloseEvent={onCloseEvent} onCloseTraining={onCloseTraining}/> 
}

function TeamPicker({ teams, selectedTeamId, onSelect }: {
  teams: Team[]
  selectedTeamId: string
  onSelect: (teamId: string) => void
}) {
  const [open, setOpen] = useState(false)
  const rootRef = useRef<HTMLDivElement>(null)
  const triggerRef = useRef<HTMLButtonElement>(null)
  const selectedTeam = teams.find((team) => team.id === selectedTeamId) ?? teams[0]

  useEffect(() => {
    if (!open) return
    const closeOnOutsideClick = (event: PointerEvent) => {
      if (event.target instanceof Node && !rootRef.current?.contains(event.target)) setOpen(false)
    }
    const closeOnEscape = (event: KeyboardEvent) => {
      if (event.key === 'Escape') {
        setOpen(false)
        triggerRef.current?.focus()
      }
    }
    document.addEventListener('pointerdown', closeOnOutsideClick)
    document.addEventListener('keydown', closeOnEscape)
    return () => {
      document.removeEventListener('pointerdown', closeOnOutsideClick)
      document.removeEventListener('keydown', closeOnEscape)
    }
  }, [open])

  if (!selectedTeam) return null

  return <div className={`team-picker${open ? ' is-open' : ''}`} ref={rootRef}>
    <span className="team-picker-label">Equipo</span>
    <button ref={triggerRef} type="button" className="team-picker-trigger" aria-haspopup="listbox" aria-expanded={open} aria-label={`Seleccionar equipo. Actual: ${selectedTeam.name} · ${selectedTeam.category}`} onClick={() => setOpen((current) => !current)}>
      <span className="team-picker-current"><strong>{selectedTeam.name}</strong><small>{selectedTeam.category}</small></span>
      <span className="material-symbols-outlined team-picker-chevron" aria-hidden="true">expand_more</span>
    </button>
    {open && <div className="team-picker-menu" role="listbox" aria-label="Equipos disponibles">
      {teams.map((team) => <button key={team.id} type="button" role="option" aria-selected={team.id === selectedTeam.id} className={`team-picker-option${team.id === selectedTeam.id ? ' selected' : ''}`} onClick={() => { onSelect(team.id); setOpen(false); triggerRef.current?.focus() }}>
        <span className="team-picker-option-copy"><strong>{team.name}</strong><small>{team.category}</small></span>
        {team.id === selectedTeam.id && <span className="material-symbols-outlined" aria-hidden="true">check</span>}
      </button>)}
    </div>}
  </div>
}

async function sendJson<T>(url: string, method: 'POST' | 'PUT' | 'DELETE', body?: unknown): Promise<T | undefined> {
  const token = await getCsrfToken()
  const response = await fetch(url, {
    method,
    credentials: 'include',
    cache: 'no-store',
    headers: { 'X-CSRF-TOKEN': token, ...(body === undefined ? {} : { 'Content-Type': 'application/json' }) },
    ...(body === undefined ? {} : { body: JSON.stringify(body) }),
  })
  const responseBody = response.status === 204 ? undefined : await response.json().catch(() => undefined)
  if (!response.ok) throw new Error(responseBody?.message ?? 'No se pudo completar la operación.')
  return responseBody as T | undefined
}

function formatDateTime(value: string) {
  return new Date(value).toLocaleString('es-ES', { weekday: 'short', day: 'numeric', month: 'short', hour: '2-digit', minute: '2-digit' })
}

function localDateTimeValue(date = new Date(Date.now() + 86400000)) {
  const offset = date.getTimezoneOffset() * 60000
  return new Date(date.getTime() - offset).toISOString().slice(0, 16)
}

function BoardAttachmentPreview({ file }: { file: BoardAttachment }) {
  if (file.contentType.startsWith('image/')) {
    return <figure className="team-board-attachment team-board-image">
      <a href={file.url} target="_blank" rel="noreferrer" aria-label={`Abrir imagen: ${file.fileName}`}>
        <img src={file.url} alt={file.fileName} loading="lazy"/>
      </a>
      <figcaption>{file.fileName}</figcaption>
    </figure>
  }

  if (file.contentType.startsWith('video/')) {
    return <figure className="team-board-attachment team-board-video">
      <video controls preload="metadata" aria-label={`Vídeo: ${file.fileName}`}>
        <source src={file.url} type={file.contentType}/>
        Tu navegador no puede reproducir este vídeo.
      </video>
      <figcaption><span>{file.fileName}</span><a href={file.url} download={file.fileName}>Descargar</a></figcaption>
    </figure>
  }

  return <a className="team-attachment" key={file.id} href={file.url} target="_blank" rel="noreferrer">
    <span className="material-symbols-outlined" aria-hidden="true">description</span>
    <span>{file.fileName}</span>
  </a>
}

function TeamCalendarView({
  calendar, calendarMonth, calendarDays, itemsByDay, currentPlan, canManage, busy, isFirstMonth, isLastMonth,
  eventFormOpen, matchFormOpen, trainingPlanOpen, editingEvent, editingTraining, players, currentUserId,
  onPrevious, onNext, onToggleEventForm, onToggleMatchForm, onTogglePlanForm, onCreateEvent, onCreateMatch,
  onSavePlan, onEditEvent, onCancelEvent, onEditTraining, onRestoreTraining, onSaveEvent, onSaveTraining,
  onSetAttendance,
  onCloseEvent, onCloseTraining,
}: {
  calendar: TeamCalendar | null
  calendarMonth: string
  calendarDays: (Date | null)[]
  itemsByDay: Map<string, CalendarItem[]>
  currentPlan?: TrainingPlan
  canManage: boolean
  busy: boolean
  isFirstMonth: boolean
  isLastMonth: boolean
  eventFormOpen: boolean
  matchFormOpen: boolean
  trainingPlanOpen: boolean
  editingEvent: CalendarItem | null
  editingTraining: CalendarItem | null
  players: TeamPlayer[]
  currentUserId: string
  onPrevious: () => void
  onNext: () => void
  onToggleEventForm: () => void
  onToggleMatchForm: () => void
  onTogglePlanForm: () => void
  onCreateEvent: (event: FormEvent<HTMLFormElement>) => void
  onCreateMatch: (event: FormEvent<HTMLFormElement>) => void
  onSavePlan: (event: FormEvent<HTMLFormElement>) => void
  onEditEvent: (item: CalendarItem) => void
  onCancelEvent: (item: CalendarItem) => void
  onEditTraining: (item: CalendarItem) => void
  onRestoreTraining: (item: CalendarItem) => void
  onSaveEvent: (event: FormEvent<HTMLFormElement>) => void
  onSaveTraining: (event: FormEvent<HTMLFormElement>) => void
  onSetAttendance: (matchId: string, attendance: string) => void
  onCloseEvent: () => void
  onCloseTraining: () => void
}) {
  const [selectedDay, setSelectedDay] = useState('')
  const activeSelectedDay = selectedDay.startsWith(`${calendarMonth}-`) ? selectedDay : ''
  const monthItems = calendarDays.flatMap((day) => day ? itemsByDay.get(localDateKey(day)) ?? [] : [])
  const selectedItems = activeSelectedDay ? itemsByDay.get(activeSelectedDay) ?? [] : monthItems
  const activePlan = currentPlan

  return <section className="team-section team-calendar">
    <div className="team-section-heading">
      <div><h2>Calendario de temporada</h2><p>{calendar ? `${new Date(`${calendar.seasonStart}T12:00:00`).toLocaleDateString('es-ES')} – ${new Date(`${calendar.seasonEnd}T12:00:00`).toLocaleDateString('es-ES')}` : 'Temporada automática de julio a junio'} · planificación, partidos y eventos.</p></div>
      <div className="team-calendar-toolbar"><button type="button" aria-label="Mes anterior" disabled={isFirstMonth} onClick={onPrevious}>‹</button><strong>{monthLabel(calendarMonth)}</strong><button type="button" aria-label="Mes siguiente" disabled={isLastMonth} onClick={onNext}>›</button></div>
    </div>

    {canManage && <div className="team-calendar-actions"><button className="team-primary-button" type="button" onClick={onTogglePlanForm}>Planificación semanal</button><button className="team-secondary-button" type="button" onClick={onToggleEventForm}>Añadir evento</button><button className="team-secondary-button" type="button" onClick={onToggleMatchForm}>Programar partido y convocatoria</button></div>}

    <div className="team-calendar-grid" role="grid" aria-label={`Calendario de ${monthLabel(calendarMonth)}`}>
      {['Lun', 'Mar', 'Mié', 'Jue', 'Vie', 'Sáb', 'Dom'].map((day) => <div className="team-calendar-weekday" role="columnheader" key={day}>{day}</div>)}
      {calendarDays.map((day, index) => {
        if (!day) return <div className="team-calendar-empty-cell" role="gridcell" key={`empty-${index}`}/> 
        const key = localDateKey(day)
        const dayItems = itemsByDay.get(key) ?? []
        return <button type="button" role="gridcell" key={key} aria-label={`${day.getDate()} de ${monthLabel(calendarMonth)}${dayItems.length ? `, ${dayItems.length} actividades` : ''}`} aria-pressed={activeSelectedDay === key} className={`team-calendar-day${activeSelectedDay === key ? ' selected' : ''}${dayItems.length ? ' has-items' : ''}`} onClick={() => setSelectedDay((current) => current === key ? '' : key)}>
          <strong className="team-calendar-day-number">{day.getDate()}</strong>
          {dayItems.length > 0 && <span className="team-calendar-day-items" aria-hidden="true">{dayItems.slice(0, 3).map((item) => <span key={item.id} className={`team-calendar-preview-item ${item.source.toLowerCase()}${item.isCancelled ? ' cancelled' : ''}`} title={`${new Date(item.startsAt).toLocaleTimeString('es-ES', { hour: '2-digit', minute: '2-digit' })} · ${item.title}`}><time>{new Date(item.startsAt).toLocaleTimeString('es-ES', { hour: '2-digit', minute: '2-digit' })}</time><span>{item.title}</span></span>)}{dayItems.length > 3 && <span className="team-calendar-more">+{dayItems.length - 3} más</span>}</span>}
        </button>
      })}
    </div>

    {canManage && <details className="team-current-plan"><summary>Planificación semanal vigente</summary>{activePlan ? <ul>{activePlan.slots.map((slot) => <li key={slot.id}><strong>{weekDays[slot.dayOfWeek]}</strong><span>{slot.startsAt.slice(0, 5)}–{slot.endsAt.slice(0, 5)}</span>{slot.venue && <small>{slot.venue}</small>}</li>)}</ul> : <p>Aún no hay planificación para esta temporada.</p>}</details>}

    <section className="team-calendar-agenda" aria-label={activeSelectedDay ? `Actividades del ${activeSelectedDay}` : 'Actividades del mes'}>
      <h3>{activeSelectedDay ? new Date(`${activeSelectedDay}T12:00:00`).toLocaleDateString('es-ES', { weekday: 'long', day: 'numeric', month: 'long' }) : 'Actividades del mes'}</h3>
      {selectedItems.length === 0 ? <p className="team-empty-inline">No hay actividades en esta fecha.</p> : selectedItems.map((item) => <article className={`team-calendar-event ${item.isCancelled ? 'cancelled' : ''}`} key={item.id}>
        <div className="team-calendar-event-time"><strong>{new Date(item.startsAt).toLocaleTimeString('es-ES', { hour: '2-digit', minute: '2-digit' })}</strong>{item.endsAt && <small>{new Date(item.endsAt).toLocaleTimeString('es-ES', { hour: '2-digit', minute: '2-digit' })}</small>}</div>
        <div className="team-calendar-event-copy"><span className={`team-calendar-event-kind ${item.source.toLowerCase()}`}>{item.isCancelled ? 'Cancelado' : item.kind}</span><strong>{item.title}</strong>{item.venue && <small>{item.venue}</small>}{item.notes && <p>{item.notes}</p>}{item.source === 'Partido' && !!item.callups?.length && <div className="team-callup-list"><strong>Convocatoria ({item.callups.length})</strong>{item.callups.map((callup) => <div key={callup.playerId}><span>{callup.jerseyNumber ?? '—'} · {callup.displayName}</span>{callup.playerId === currentUserId ? callup.attendance === 'Pendiente' ? <span className="team-attendance-actions"><button type="button" onClick={() => onSetAttendance(item.id, 'Confirmada')}>Confirmar</button><button type="button" onClick={() => onSetAttendance(item.id, 'No disponible')}>No disponible</button></span> : <span className="team-player-position">{callup.attendance}</span> : <span className="team-attendance-state">{callup.attendance}</span>}</div>)}</div>}</div>
      {canManage && !item.isCancelled && item.source === 'Planificacion' && <div className="team-calendar-event-actions"><button type="button" onClick={() => onEditTraining(item)}>Editar fecha</button></div>}
      {canManage && !item.isCancelled && item.source === 'Evento' && <div className="team-calendar-event-actions"><button type="button" onClick={() => onEditEvent(item)}>Editar</button><button type="button" onClick={() => onCancelEvent(item)}>Cancelar</button></div>}
        {canManage && item.isCancelled && item.source === 'Planificacion' && <button className="team-calendar-event-actions" type="button" onClick={() => onRestoreTraining(item)}>Restaurar sesión</button>}
      </article>)}
    </section>

    {trainingPlanOpen && canManage && <form className="team-training-plan-form" onSubmit={onSavePlan}>
      <div className="team-section-heading"><div><h3>Planificación semanal</h3><p>Se aplicará desde la fecha efectiva que elijas; las fechas anteriores conservan su horario.</p></div></div>
      <label className="team-form-effective"><span>Fecha efectiva</span><input name="effectiveFrom" type="date" min={calendar?.seasonStart} max={calendar?.seasonEnd} defaultValue={calendarMonth + '-01'} required/></label>
      <div className="team-training-days">{weekDays.slice(1).concat(weekDays.slice(0, 1)).map((day, index) => { const dayOfWeek = (index + 1) % 7; const current = activePlan?.slots.find((slot) => slot.dayOfWeek === dayOfWeek); return <fieldset key={day} className="team-training-day"><legend>{day}</legend><label><span>Desde</span><input aria-label={`${day} desde`} name={`start-${dayOfWeek}`} type="time" defaultValue={current?.startsAt.slice(0, 5)}/></label><label><span>Hasta</span><input aria-label={`${day} hasta`} name={`end-${dayOfWeek}`} type="time" defaultValue={current?.endsAt.slice(0, 5)}/></label><label><span>Lugar (opcional)</span><input aria-label={`${day} lugar`} name={`venue-${dayOfWeek}`} defaultValue={current?.venue ?? ''} maxLength={200}/></label></fieldset>})}</div>
      <button className="team-primary-button" disabled={busy}>{busy ? 'Guardando…' : 'Guardar planificación'}</button>
    </form>}

    {eventFormOpen && canManage && <form className="team-data-form" onSubmit={onCreateEvent}><label><span>Actividad</span><input name="title" required maxLength={160} placeholder="Reunión de equipo"/></label><label><span>Tipo</span><select name="kind"><option>Evento</option><option>Entrenamiento</option></select></label><label><span>Inicio</span><input name="startsAt" type="datetime-local" defaultValue={localDateTimeValue()} required/></label><label><span>Fin</span><input name="endsAt" type="datetime-local"/></label><label><span>Lugar (opcional)</span><input name="venue" maxLength={200}/></label><label className="team-form-wide"><span>Notas</span><input name="notes" maxLength={1000}/></label><button className="team-primary-button" disabled={busy}>{busy ? 'Guardando…' : 'Guardar evento'}</button></form>}

    {matchFormOpen && canManage && <form className="team-data-form" onSubmit={onCreateMatch}><label><span>Rival</span><input name="opponent" required maxLength={160}/></label><label><span>Jornada</span><input name="round" maxLength={80}/></label><label><span>Fecha y hora</span><input name="startsAt" type="datetime-local" defaultValue={localDateTimeValue()} required/></label><label><span>Pabellón</span><input name="venue" required maxLength={200}/></label><fieldset className="team-form-wide"><legend>Convocar jugadores</legend>{players.map((player) => <label key={player.userId}><input type="checkbox" name="calledUpPlayerIds" value={player.userId}/>{player.displayName}</label>)}</fieldset><button className="team-primary-button" disabled={busy}>{busy ? 'Guardando…' : 'Guardar partido y convocatoria'}</button></form>}

    {editingEvent && <dialog open className="team-calendar-dialog"><form className="team-data-form" onSubmit={onSaveEvent}><h3>Editar evento</h3><label><span>Actividad</span><input name="title" defaultValue={editingEvent.title} required maxLength={160}/></label><label><span>Tipo</span><select name="kind" defaultValue={editingEvent.kind}><option>Evento</option><option>Entrenamiento</option></select></label><label><span>Inicio</span><input name="startsAt" type="datetime-local" defaultValue={localDateTimeValue(new Date(editingEvent.startsAt))} required/></label><label><span>Fin</span><input name="endsAt" type="datetime-local" defaultValue={editingEvent.endsAt ? localDateTimeValue(new Date(editingEvent.endsAt)) : ''}/></label><label><span>Lugar</span><input name="venue" defaultValue={editingEvent.venue} maxLength={200}/></label><label className="team-form-wide"><span>Notas</span><input name="notes" defaultValue={editingEvent.notes} maxLength={1000}/></label><button className="team-primary-button" disabled={busy}>Guardar cambios</button><button className="team-secondary-button" type="button" onClick={onCloseEvent}>Cerrar</button></form></dialog>}

    {editingTraining && <dialog open className="team-calendar-dialog" aria-labelledby="team-training-exception-title"><form className="team-training-exception-form" onSubmit={onSaveTraining}>
      <header className="team-training-exception-heading"><span className="material-symbols-outlined" aria-hidden="true">edit_calendar</span><div><h3 id="team-training-exception-title">Modificar entrenamiento</h3><p>El cambio solo afectará a esta fecha.</p></div><button className="team-calendar-dialog-close" type="button" aria-label="Cerrar" onClick={onCloseTraining}>×</button></header>
      <p className="team-training-exception-date">{editingTraining.trainingDate && new Date(`${editingTraining.trainingDate}T12:00:00`).toLocaleDateString('es-ES', { weekday: 'long', day: 'numeric', month: 'long', year: 'numeric' })}</p>
      <div className="team-training-exception-fields"><label><span>Hora de inicio</span><input name="startsAt" type="time" defaultValue={new Date(editingTraining.startsAt).toLocaleTimeString('es-ES', { hour: '2-digit', minute: '2-digit' })}/></label><label><span>Hora de fin</span><input name="endsAt" type="time" defaultValue={editingTraining.endsAt ? new Date(editingTraining.endsAt).toLocaleTimeString('es-ES', { hour: '2-digit', minute: '2-digit' }) : ''}/></label><label className="team-training-exception-venue"><span>Lugar <small>(opcional)</small></span><input name="venue" defaultValue={editingTraining.venue} maxLength={200} placeholder="Pabellón o instalación"/></label></div>
      <label className="team-training-cancel-option"><input name="cancelled" type="checkbox"/><span className="team-training-cancel-checkbox material-symbols-outlined" aria-hidden="true">check</span><span className="team-training-cancel-copy"><strong>Cancelar este entrenamiento</strong><small>La planificación semanal seguirá igual para el resto de fechas.</small></span></label>
      <footer className="team-training-exception-actions"><button className="team-secondary-button" type="button" onClick={onCloseTraining}>Cerrar</button><button className="team-primary-button" disabled={busy}>{busy ? 'Guardando…' : 'Guardar cambios'}</button></footer>
    </form></dialog>}
  </section>
}

export default function TeamPage({ theme, onToggleTheme, user }: { theme: Theme; onToggleTheme: () => void; user: SessionUser }) {
  const [teams, setTeams] = useState<Team[]>([])
  const [selectedTeamId, setSelectedTeamId] = useState('')
  const [space, setSpace] = useState<TeamSpace | null>(null)
  const [tab, setTab] = useState<Tab>('plantilla')
  const [loadingTeams, setLoadingTeams] = useState(true)
  const [loadingSpace, setLoadingSpace] = useState(false)
  const [error, setError] = useState('')
  const [notice, setNotice] = useState('')
  const [query, setQuery] = useState('')
  const [candidates, setCandidates] = useState<Candidate[]>([])
  const [selectedCandidate, setSelectedCandidate] = useState<Candidate | null>(null)
  const [searchingCandidates, setSearchingCandidates] = useState(false)
  const [candidateMenuOpen, setCandidateMenuOpen] = useState(false)
  const [activeCandidateIndex, setActiveCandidateIndex] = useState(-1)
  const candidatePickerRef = useRef<HTMLDivElement>(null)
  const [selectedPlayer, setSelectedPlayer] = useState('')
  const [jerseyNumber, setJerseyNumber] = useState('')
  const [position, setPosition] = useState('Universal')
  const [busy, setBusy] = useState(false)
  const [boardText, setBoardText] = useState('')
  const [boardFiles, setBoardFiles] = useState<File[]>([])
  const [matchFormOpen, setMatchFormOpen] = useState(false)
  const [eventFormOpen, setEventFormOpen] = useState(false)
  const [trainingPlanOpen, setTrainingPlanOpen] = useState(false)
  const [calendar, setCalendar] = useState<TeamCalendar | null>(null)
  const [calendarMonth, setCalendarMonth] = useState(() => monthDateKey(new Date()))
  const [editingEvent, setEditingEvent] = useState<CalendarItem | null>(null)
  const [editingTraining, setEditingTraining] = useState<CalendarItem | null>(null)
  const [now] = useState(() => Date.now())
  const activeTeamIdRef = useRef('')
  const selectionVersionRef = useRef(0)
  const spaceRequestIdRef = useRef(0)
  const calendarRequestIdRef = useRef(0)

  const isCurrentSelection = (teamId: string, version: number) =>
    activeTeamIdRef.current === teamId && selectionVersionRef.current === version

  const invalidateTeamRequests = () => {
    selectionVersionRef.current += 1
    spaceRequestIdRef.current += 1
    calendarRequestIdRef.current += 1
  }

  const loadTeams = useCallback(async () => {
    setError('')
    setLoadingTeams(true)
    try {
      const isAdministrator = user.roles.includes('Administrador')
      const response = await fetch(isAdministrator ? '/api/v1/admin/teams' : '/api/v1/my-teams', { credentials: 'include', cache: 'no-store' })
      if (!response.ok) throw new Error(response.status === 401 ? 'Inicia sesión para consultar tus equipos.' : 'No se pudieron cargar tus equipos.')
      const result = isAdministrator
        ? (await response.json() as { id: string; name: string; category: string; isActive: boolean }[])
          .filter((team) => team.isActive)
          .map(({ id, name, category }) => ({ id, name, category, access: 'Administrador' as const }))
        : await response.json() as Team[]
      setTeams(result)
      const currentTeamId = activeTeamIdRef.current
      const nextTeamId = result.some((team) => team.id === currentTeamId) ? currentTeamId : result[0]?.id ?? ''
      if (nextTeamId !== currentTeamId) invalidateTeamRequests()
      activeTeamIdRef.current = nextTeamId
      setSelectedTeamId(nextTeamId)
    } catch (loadError) { setError(loadError instanceof Error ? loadError.message : 'No se pudieron cargar tus equipos.') }
    finally { setLoadingTeams(false) }
  }, [user.roles])

  const loadSpace = useCallback(async (teamId: string) => {
    if (!teamId) {
      if (!activeTeamIdRef.current) { setSpace(null); setLoadingSpace(false) }
      return
    }
    const selectionVersion = selectionVersionRef.current
    const requestId = ++spaceRequestIdRef.current
    const isCurrentRequest = () => isCurrentSelection(teamId, selectionVersion) && spaceRequestIdRef.current === requestId
    if (!isCurrentRequest()) return
    setLoadingSpace(true)
    setError('')
    try {
      const response = await fetch(`/api/v1/teams/${teamId}/space`, { credentials: 'include', cache: 'no-store' })
      if (!response.ok) throw new Error(response.status === 404 ? 'No tienes acceso a este equipo o ya no está disponible.' : 'No se pudo cargar el espacio del equipo.')
      const result = await response.json() as TeamSpace
      if (isCurrentRequest()) setSpace(result)
    } catch (loadError) {
      if (isCurrentRequest()) {
        setSpace(null)
        setError(loadError instanceof Error ? loadError.message : 'No se pudo cargar el espacio del equipo.')
      }
    } finally {
      if (isCurrentRequest()) setLoadingSpace(false)
    }
  }, [])

  useEffect(() => {
    const timer = window.setTimeout(() => { void loadTeams() }, 0)
    return () => window.clearTimeout(timer)
  }, [loadTeams])
  useEffect(() => {
    const timer = window.setTimeout(() => { void loadSpace(selectedTeamId) }, 0)
    return () => window.clearTimeout(timer)
  }, [loadSpace, selectedTeamId])

  useEffect(() => {
    if (!selectedTeamId || tab !== 'calendario-v2') return
    const selectionVersion = selectionVersionRef.current
    const requestId = ++calendarRequestIdRef.current
    let active = true
    const isCurrentRequest = () => active && isCurrentSelection(selectedTeamId, selectionVersion) && calendarRequestIdRef.current === requestId
    const timer = window.setTimeout(() => {
      void fetch(`/api/v1/teams/${selectedTeamId}/calendar?year=${calendarMonth.split('-')[0]}&month=${calendarMonth.split('-')[1]}`, { credentials: 'include', cache: 'no-store' })
        .then(async (response) => {
          if (!response.ok) throw new Error('No se pudo cargar el calendario.')
          const result = await response.json() as TeamCalendar
          if (isCurrentRequest()) {
            setCalendar(result)
            setCalendarMonth(result.month.slice(0, 7))
          }
        })
        .catch((loadError: unknown) => {
          if (isCurrentRequest()) setError(loadError instanceof Error ? loadError.message : 'No se pudo cargar el calendario.')
        })
    }, 0)
    return () => { active = false; window.clearTimeout(timer) }
  }, [calendarMonth, selectedTeamId, tab])

  useEffect(() => {
    if (!space?.canManage || tab !== 'plantilla' || query.trim().length < 2 || selectedPlayer) return
    let active = true
    const timer = window.setTimeout(() => {
      setSearchingCandidates(true)
      void fetch(`/api/v1/teams/${space.team.id}/players/search?query=${encodeURIComponent(query.trim())}`, { credentials: 'include', cache: 'no-store' })
        .then(async (response) => {
          if (!response.ok) throw new Error('No se pudieron buscar jugadores.')
          if (active) {
            setCandidates(await response.json() as Candidate[])
            setActiveCandidateIndex(-1)
            setCandidateMenuOpen(true)
          }
        })
        .catch((searchError: unknown) => { if (active) setError(searchError instanceof Error ? searchError.message : 'No se pudieron buscar jugadores.') })
        .finally(() => { if (active) setSearchingCandidates(false) })
    }, 250)
    return () => { active = false; window.clearTimeout(timer) }
  }, [query, selectedPlayer, space?.canManage, space?.team.id, tab])

  useEffect(() => {
    if (!candidateMenuOpen) return
    const closeOnOutsideClick = (event: PointerEvent) => {
      if (event.target instanceof Node && !candidatePickerRef.current?.contains(event.target)) setCandidateMenuOpen(false)
    }
    const closeOnEscape = (event: KeyboardEvent) => {
      if (event.key === 'Escape') setCandidateMenuOpen(false)
    }
    document.addEventListener('pointerdown', closeOnOutsideClick)
    document.addEventListener('keydown', closeOnEscape)
    return () => {
      document.removeEventListener('pointerdown', closeOnOutsideClick)
      document.removeEventListener('keydown', closeOnEscape)
    }
  }, [candidateMenuOpen])

  const selectCandidate = (candidate: Candidate) => {
    setSelectedPlayer(candidate.userId)
    setSelectedCandidate(candidate)
    setQuery('')
    setCandidates([])
    setCandidateMenuOpen(false)
    setSearchingCandidates(false)
    setActiveCandidateIndex(-1)
  }

  const handleCandidateSearchKeyDown = (event: React.KeyboardEvent<HTMLInputElement>) => {
    if (event.key === 'ArrowDown' && candidates.length > 0) {
      event.preventDefault()
      setCandidateMenuOpen(true)
      setActiveCandidateIndex((index) => index < 0 ? 0 : (index + 1) % candidates.length)
      return
    }
    if (event.key === 'Escape') {
      setCandidateMenuOpen(false)
      return
    }
    if (query.trim().length < 2 || candidates.length === 0) return
    if (event.key === 'ArrowUp') {
      event.preventDefault()
      setCandidateMenuOpen(true)
      setActiveCandidateIndex((index) => index < 0 ? candidates.length - 1 : (index - 1 + candidates.length) % candidates.length)
    } else if (event.key === 'Enter' && candidateMenuOpen) {
      event.preventDefault()
      selectCandidate(activeCandidateIndex < 0 ? candidates[0] : candidates[activeCandidateIndex])
    }
  }

  const clearSelectedCandidate = () => {
    setSelectedPlayer('')
    setSelectedCandidate(null)
    setQuery('')
    setCandidates([])
    setSearchingCandidates(false)
    setCandidateMenuOpen(false)
    setActiveCandidateIndex(-1)
  }

  const clearPlayerCandidate = clearSelectedCandidate
  const searchPlayerCandidates = (value: string) => {
    setQuery(value)
    setSelectedPlayer('')
    setSelectedCandidate(null)
    setCandidates([])
    setActiveCandidateIndex(-1)
    setSearchingCandidates(value.trim().length >= 2)
    setCandidateMenuOpen(value.trim().length >= 2)
  }

  const upcomingMatch = useMemo(() => space?.matches.find((match) => new Date(match.startsAt).getTime() >= now) ?? space?.matches[0] ?? null, [now, space?.matches])
  const confirmedCount = upcomingMatch?.callups.filter((callup) => callup.attendance === 'Confirmada').length ?? 0

  const refreshSpace = async (teamId: string, selectionVersion: number) => {
    if (teamId !== activeTeamIdRef.current || selectionVersion !== selectionVersionRef.current) return false
    await loadSpace(teamId)
    return isCurrentSelection(teamId, selectionVersion)
  }

  const addPlayer = async (event: FormEvent<HTMLFormElement>) => {
    event.preventDefault()
    if (!space || space.team.id !== activeTeamIdRef.current || !selectedPlayer) return
    const teamId = space.team.id
    const selectionVersion = selectionVersionRef.current
    setBusy(true); setError(''); setNotice('')
    try {
      await sendJson(`/api/v1/teams/${teamId}/players`, 'POST', {
        userId: selectedPlayer, jerseyNumber: jerseyNumber ? Number(jerseyNumber) : null, position,
      })
      if (!isCurrentSelection(teamId, selectionVersion)) return
      clearSelectedCandidate(); setJerseyNumber(''); setPosition('Universal')
      if (!await refreshSpace(teamId, selectionVersion)) return
      setNotice('Jugador añadido a la plantilla.')
    } catch (actionError) { if (isCurrentSelection(teamId, selectionVersion)) setError(actionError instanceof Error ? actionError.message : 'No se pudo añadir al jugador.') }
    finally { if (isCurrentSelection(teamId, selectionVersion)) setBusy(false) }
  }

  const removePlayer = async (player: TeamPlayer) => {
    if (!space || space.team.id !== activeTeamIdRef.current || !window.confirm(`¿Retirar a ${player.displayName} de ${space.team.name}?`)) return
    const teamId = space.team.id
    const selectionVersion = selectionVersionRef.current
    setError(''); setNotice('')
    try {
      await sendJson(`/api/v1/teams/${teamId}/players/${player.userId}`, 'DELETE')
      if (!isCurrentSelection(teamId, selectionVersion)) return
      if (!await refreshSpace(teamId, selectionVersion)) return
      setNotice(`${player.displayName} ya no pertenece a este equipo.`)
    } catch (actionError) { if (isCurrentSelection(teamId, selectionVersion)) setError(actionError instanceof Error ? actionError.message : 'No se pudo retirar al jugador.') }
  }

  const updatePlayerStats = async (player: TeamPlayer, form: HTMLFormElement) => {
    if (!space || space.team.id !== activeTeamIdRef.current) return
    const teamId = space.team.id
    const selectionVersion = selectionVersionRef.current
    const formData = new FormData(form)
    try {
      await sendJson(`/api/v1/teams/${teamId}/players/${player.userId}`, 'PUT', {
        matchesPlayed: Number(formData.get('matchesPlayed')), minutesPlayed: Number(formData.get('minutesPlayed')),
        goals: Number(formData.get('goals')), saves: Number(formData.get('saves')),
      })
      if (!isCurrentSelection(teamId, selectionVersion)) return
      if (!await refreshSpace(teamId, selectionVersion)) return
      setNotice(`Estadísticas de ${player.displayName} actualizadas.`)
    } catch (actionError) { if (isCurrentSelection(teamId, selectionVersion)) setError(actionError instanceof Error ? actionError.message : 'No se pudieron guardar las estadísticas.') }
  }

  const createMatch = async (event: FormEvent<HTMLFormElement>) => {
    event.preventDefault()
    if (!space || space.team.id !== activeTeamIdRef.current) return
    const teamId = space.team.id
    const selectionVersion = selectionVersionRef.current
    const form = new FormData(event.currentTarget)
    setBusy(true); setError(''); setNotice('')
    try {
      await sendJson(`/api/v1/teams/${teamId}/matches`, 'POST', {
        opponent: form.get('opponent'), startsAt: new Date(String(form.get('startsAt'))).toISOString(),
        venue: form.get('venue'), round: form.get('round'),
        calledUpPlayerIds: form.getAll('calledUpPlayerIds').map(String),
      })
      if (!isCurrentSelection(teamId, selectionVersion)) return
      setMatchFormOpen(false)
      if (!await refreshSpace(teamId, selectionVersion)) return
      setNotice('Partido y convocatoria guardados.')
    } catch (actionError) { if (isCurrentSelection(teamId, selectionVersion)) setError(actionError instanceof Error ? actionError.message : 'No se pudo crear el partido.') }
    finally { if (isCurrentSelection(teamId, selectionVersion)) setBusy(false) }
  }

  const setAttendance = async (matchId: string, attendance: string) => {
    if (!space || space.team.id !== activeTeamIdRef.current) return
    const teamId = space.team.id
    const selectionVersion = selectionVersionRef.current
    try {
      await sendJson(`/api/v1/teams/${teamId}/matches/${matchId}/attendance`, 'PUT', { attendance })
      if (!isCurrentSelection(teamId, selectionVersion)) return
      if (!await refreshSpace(teamId, selectionVersion)) return
      setNotice('Tu respuesta de asistencia se ha guardado.')
    } catch (actionError) { if (isCurrentSelection(teamId, selectionVersion)) setError(actionError instanceof Error ? actionError.message : 'No se pudo actualizar la asistencia.') }
  }

  const createEvent = async (event: FormEvent<HTMLFormElement>) => {
    event.preventDefault()
    if (!space || space.team.id !== activeTeamIdRef.current) return
    const teamId = space.team.id
    const selectionVersion = selectionVersionRef.current
    const form = new FormData(event.currentTarget)
    setBusy(true); setError(''); setNotice('')
    try {
      await sendJson(`/api/v1/teams/${teamId}/events`, 'POST', {
        title: form.get('title'), kind: form.get('kind'), startsAt: new Date(String(form.get('startsAt'))).toISOString(),
        endsAt: form.get('endsAt') ? new Date(String(form.get('endsAt'))).toISOString() : null,
        venue: form.get('venue'), notes: form.get('notes'),
      })
      if (!isCurrentSelection(teamId, selectionVersion)) return
      setEventFormOpen(false)
      if (!await refreshSpace(teamId, selectionVersion)) return
      setNotice('Actividad añadida al calendario.')
      void loadCalendar(teamId, selectionVersion).catch((loadError: unknown) => {
        if (isCurrentSelection(teamId, selectionVersion)) setError(loadError instanceof Error ? loadError.message : 'No se pudo cargar el calendario.')
      })
    } catch (actionError) { if (isCurrentSelection(teamId, selectionVersion)) setError(actionError instanceof Error ? actionError.message : 'No se pudo añadir la actividad.') }
    finally { if (isCurrentSelection(teamId, selectionVersion)) setBusy(false) }
  }

  const loadCalendar = async (teamId: string, selectionVersion: number) => {
    if (!space || space.team.id !== teamId) return false
    const requestId = ++calendarRequestIdRef.current
    const [year, month] = calendarMonth.split('-')
    const response = await fetch(`/api/v1/teams/${teamId}/calendar?year=${year}&month=${month}`, { credentials: 'include', cache: 'no-store' })
    if (!isCurrentSelection(teamId, selectionVersion) || calendarRequestIdRef.current !== requestId) return false
    if (!response.ok) throw new Error('No se pudo cargar el calendario.')
    const result = await response.json() as TeamCalendar
    if (!isCurrentSelection(teamId, selectionVersion) || calendarRequestIdRef.current !== requestId) return false
    setCalendar(result)
    return true
  }

  const saveEvent = async (event: FormEvent<HTMLFormElement>) => {
    event.preventDefault()
    if (!space || space.team.id !== activeTeamIdRef.current || !editingEvent) return
    const teamId = space.team.id
    const selectionVersion = selectionVersionRef.current
    const form = new FormData(event.currentTarget)
    setBusy(true); setError(''); setNotice('')
    try {
      await sendJson(`/api/v1/teams/${teamId}/events/${editingEvent.id}`, 'PUT', {
        title: form.get('title'), kind: form.get('kind'), startsAt: new Date(String(form.get('startsAt'))).toISOString(),
        endsAt: form.get('endsAt') ? new Date(String(form.get('endsAt'))).toISOString() : null,
        venue: form.get('venue'), notes: form.get('notes'),
      })
      if (!isCurrentSelection(teamId, selectionVersion)) return
      setEditingEvent(null)
      if (!await loadCalendar(teamId, selectionVersion)) return
      setNotice('Evento actualizado.')
    } catch (actionError) { if (isCurrentSelection(teamId, selectionVersion)) setError(actionError instanceof Error ? actionError.message : 'No se pudo actualizar el evento.') }
    finally { if (isCurrentSelection(teamId, selectionVersion)) setBusy(false) }
  }

  const saveCalendarTrainingException = (event: FormEvent<HTMLFormElement>) => {
    void updateTrainingDate(event)
  }

  const cancelCalendarEvent = async (item: CalendarItem) => {
    if (!space || space.team.id !== activeTeamIdRef.current || !window.confirm(`¿Cancelar «${item.title}»?`)) return
    const teamId = space.team.id
    const selectionVersion = selectionVersionRef.current
    try {
      await sendJson(`/api/v1/teams/${teamId}/events/${item.id}`, 'DELETE')
      if (!isCurrentSelection(teamId, selectionVersion)) return
      if (!await loadCalendar(teamId, selectionVersion)) return
      setNotice('Evento cancelado.')
    } catch (actionError) { if (isCurrentSelection(teamId, selectionVersion)) setError(actionError instanceof Error ? actionError.message : 'No se pudo cancelar el evento.') }
  }

  const saveTrainingPlan = async (event: FormEvent<HTMLFormElement>) => {
    event.preventDefault()
    if (!space || space.team.id !== activeTeamIdRef.current) return
    const teamId = space.team.id
    const selectionVersion = selectionVersionRef.current
    const form = new FormData(event.currentTarget)
    const slots = [0, 1, 2, 3, 4, 5, 6].flatMap((dayOfWeek) => {
      const startsAt = String(form.get(`start-${dayOfWeek}`) ?? '')
      const endsAt = String(form.get(`end-${dayOfWeek}`) ?? '')
      if (!startsAt || !endsAt) return []
      return [{ dayOfWeek, startsAt: `${startsAt}:00`, endsAt: `${endsAt}:00`, venue: String(form.get(`venue-${dayOfWeek}`) ?? '') }]
    })
    setBusy(true); setError(''); setNotice('')
    try {
      await sendJson(`/api/v1/teams/${teamId}/training-plan`, 'PUT', { effectiveFrom: form.get('effectiveFrom'), slots })
      if (!isCurrentSelection(teamId, selectionVersion)) return
      setTrainingPlanOpen(false)
      if (!await loadCalendar(teamId, selectionVersion)) return
      setNotice('La planificación semanal se ha guardado.')
    } catch (actionError) { if (isCurrentSelection(teamId, selectionVersion)) setError(actionError instanceof Error ? actionError.message : 'No se pudo guardar la planificación.') }
    finally { if (isCurrentSelection(teamId, selectionVersion)) setBusy(false) }
  }

  const updateTrainingDate = async (event: FormEvent<HTMLFormElement>) => {
    event.preventDefault()
    if (!space || space.team.id !== activeTeamIdRef.current || !editingTraining?.trainingSlotId || !editingTraining.trainingDate) return
    const teamId = space.team.id
    const selectionVersion = selectionVersionRef.current
    const form = new FormData(event.currentTarget)
    const isCancelled = form.get('cancelled') === 'on'
    setBusy(true); setError(''); setNotice('')
    try {
      if (isCancelled) {
        await sendJson(`/api/v1/teams/${teamId}/training-plan/${editingTraining.trainingSlotId}/exceptions/${editingTraining.trainingDate}`, 'PUT', { isCancelled: true })
      } else {
        await sendJson(`/api/v1/teams/${teamId}/training-plan/${editingTraining.trainingSlotId}/exceptions/${editingTraining.trainingDate}`, 'PUT', {
          isCancelled: false, startsAt: `${String(form.get('startsAt'))}:00`, endsAt: `${String(form.get('endsAt'))}:00`, venue: form.get('venue'),
        })
      }
      if (!isCurrentSelection(teamId, selectionVersion)) return
      setEditingTraining(null)
      if (!await loadCalendar(teamId, selectionVersion)) return
      setNotice(isCancelled ? 'Entrenamiento cancelado solo para esta fecha.' : 'Entrenamiento modificado solo para esta fecha.')
    } catch (actionError) { if (isCurrentSelection(teamId, selectionVersion)) setError(actionError instanceof Error ? actionError.message : 'No se pudo modificar el entrenamiento.') }
    finally { if (isCurrentSelection(teamId, selectionVersion)) setBusy(false) }
  }

  const navigateCalendar = (offset: number) => {
    const [year, month] = calendarMonth.split('-').map(Number)
    const next = new Date(year, month - 1 + offset, 1)
    const nextKey = monthDateKey(next)
    const firstMonth = calendar?.seasonStart.slice(0, 7) ?? seasonStartKey().slice(0, 7)
    const lastMonth = calendar?.seasonEnd.slice(0, 7) ?? seasonEndKey().slice(0, 7)
    if (nextKey >= firstMonth && nextKey <= lastMonth) setCalendarMonth(nextKey)
  }

  const isCalendarFirstMonth = calendarMonth === (calendar?.seasonStart.slice(0, 7) ?? seasonStartKey().slice(0, 7))
  const isCalendarLastMonth = calendarMonth === (calendar?.seasonEnd.slice(0, 7) ?? seasonEndKey().slice(0, 7))

  const restoreTrainingDate = async (item: CalendarItem) => {
    if (!space || space.team.id !== activeTeamIdRef.current || !item.trainingSlotId || !item.trainingDate) return
    const teamId = space.team.id
    const selectionVersion = selectionVersionRef.current
    try {
      await sendJson(`/api/v1/teams/${teamId}/training-plan/${item.trainingSlotId}/exceptions/${item.trainingDate}`, 'DELETE')
      if (!isCurrentSelection(teamId, selectionVersion)) return
      if (!await loadCalendar(teamId, selectionVersion)) return
      setNotice('Se ha restaurado la planificación semanal para esta fecha.')
    } catch (actionError) { if (isCurrentSelection(teamId, selectionVersion)) setError(actionError instanceof Error ? actionError.message : 'No se pudo restaurar el entrenamiento.') }
  }

  const selectTeam = (teamId: string) => {
    if (teamId === activeTeamIdRef.current) return
    activeTeamIdRef.current = teamId
    invalidateTeamRequests()
    setSelectedTeamId(teamId)
    setSpace(null)
    setLoadingSpace(Boolean(teamId))
    setCalendar(null)
    setCalendarMonth(monthDateKey(new Date()))
    setEditingEvent(null)
    setEditingTraining(null)
    setMatchFormOpen(false)
    setEventFormOpen(false)
    setTrainingPlanOpen(false)
    setBoardText('')
    setBoardFiles([])
    setBusy(false)
    clearSelectedCandidate()
    setJerseyNumber('')
    setPosition('Universal')
    setTab('plantilla')
    setError('')
    setNotice('')
  }

  const createBoardPost = async (event: FormEvent<HTMLFormElement>) => {
    event.preventDefault()
    if (!space || space.team.id !== activeTeamIdRef.current) return
    const teamId = space.team.id
    const selectionVersion = selectionVersionRef.current
    const text = boardText
    const files = boardFiles
    setBusy(true); setError(''); setNotice('')
    try {
      const token = await getCsrfToken()
      const body = new FormData()
      body.append('content', text)
      files.forEach((file) => body.append('files', file))
      const response = await fetch(`/api/v1/teams/${teamId}/board`, {
        method: 'POST', credentials: 'include', cache: 'no-store', headers: { 'X-CSRF-TOKEN': token }, body,
      })
      const result = await response.json().catch(() => undefined)
      if (!response.ok) throw new Error(result?.message ?? 'No se pudo publicar en la pizarra.')
      if (!isCurrentSelection(teamId, selectionVersion)) return
      setBoardText(''); setBoardFiles([])
      if (!await refreshSpace(teamId, selectionVersion)) return
      setNotice('Mensaje publicado en la pizarra del equipo.')
    } catch (actionError) { if (isCurrentSelection(teamId, selectionVersion)) setError(actionError instanceof Error ? actionError.message : 'No se pudo publicar en la pizarra.') }
    finally { if (isCurrentSelection(teamId, selectionVersion)) setBusy(false) }
  }

  const tabs: { id: Tab; label: string; icon: string }[] = [
    { id: 'plantilla', label: `Plantilla (${space?.players.length ?? 0})`, icon: 'groups' },
    { id: 'pizarra', label: 'Pizarra', icon: 'developer_board' },
    { id: 'calendario-v2', label: 'Calendario', icon: 'calendar_month' },
    { id: 'estadisticas', label: 'Estadísticas', icon: 'monitoring' },
  ]

  const calendarDays = useMemo(() => monthDays(calendarMonth), [calendarMonth])
  const calendarItemsByDay = useMemo(() => {
    const grouped = new Map<string, CalendarItem[]>()
    for (const item of calendar?.items ?? []) {
      const key = item.trainingDate ?? localDateKey(new Date(item.startsAt))
      grouped.set(key, [...(grouped.get(key) ?? []), item])
    }
    return grouped
  }, [calendar])
  const effectivePlans = calendar?.plans.filter((plan) => plan.effectiveFrom <= `${calendarMonth}-31`) ?? []
  const currentPlan = effectivePlans[effectivePlans.length - 1]

  return <main className="team-page">
    <SiteHeader theme={theme} onToggleTheme={onToggleTheme} user={user}/>
    <div className="team-shell">
      <header className="team-heading"><div className="team-heading-icon material-symbols-outlined" aria-hidden="true">sports_handball</div><div className="team-heading-copy"><span className="section-kicker">ESPACIO PRIVADO DEL EQUIPO</span><h1>{space?.team.name ?? 'Mis equipos'}</h1><p>Plantilla, convocatorias y actividad semanal.</p></div>
        {teams.length > 1 && <TeamPicker teams={teams} selectedTeamId={selectedTeamId} onSelect={selectTeam}/>}
      </header>
      {error && <div className="team-message team-error" role="alert">{error}</div>}
      {notice && <div className="team-message team-success" role="status">{notice}</div>}
      {loadingTeams ? <div className="team-empty" role="status">Cargando tus equipos…</div> : teams.length === 0 ? <section className="team-empty-card"><span className="material-symbols-outlined" aria-hidden="true">groups</span><h2>Aún no tienes equipos asignados</h2><p>Cuando Administración te añada como jugador o miembro del cuerpo técnico, aquí aparecerá el espacio de tu equipo.</p></section> : <>
        {loadingSpace || !space ? <div className="team-empty" role="status">{loadingSpace ? 'Cargando equipo…' : 'No se pudo abrir este equipo.'}</div> : <>
          <section className="team-overview">
            <div className="team-overview-info"><span className="team-category-chip"><span className="material-symbols-outlined" aria-hidden="true">verified</span>{space.team.category}</span><h2>{space.team.name}</h2><p>Tu espacio de coordinación y actividad deportiva.</p></div>
            <div className="team-overview-metrics"><div><span>Jugadores</span><strong>{space.players.length}</strong></div><div><span>Próximo partido</span><strong>{upcomingMatch ? formatDateTime(upcomingMatch.startsAt) : 'Por programar'}</strong></div><div><span>Convocados</span><strong>{upcomingMatch ? `${confirmedCount}/${upcomingMatch.callups.length}` : '—'}</strong></div></div>
          </section>
          {upcomingMatch && <section className="team-next-match"><div className="team-match-icon material-symbols-outlined" aria-hidden="true">event_upcoming</div><div className="team-next-match-copy"><div><span className="team-match-round">{upcomingMatch.round || 'Próximo partido'}</span><span className="team-match-date">{formatDateTime(upcomingMatch.startsAt)} · {upcomingMatch.venue}</span></div><h2>{space.team.name} <span>VS</span> {upcomingMatch.opponent}</h2></div><div className="team-match-quorum"><strong>{confirmedCount}<small>/{upcomingMatch.callups.length}</small></strong><span>confirmados</span></div></section>}
          <nav className="team-tabs" aria-label="Secciones del equipo">{tabs.map((item) => <button type="button" className={tab === item.id ? 'selected' : ''} key={item.id} onClick={() => setTab(item.id)}><span className="material-symbols-outlined" aria-hidden="true">{item.icon}</span>{item.label}</button>)}</nav>

          {tab === 'calendario-v2' && space && renderCalendar(calendar, calendarMonth, calendarDays, calendarItemsByDay, currentPlan, space.canManage, busy, isCalendarFirstMonth, isCalendarLastMonth, eventFormOpen, matchFormOpen, trainingPlanOpen, editingEvent, editingTraining, space.players, user.id, () => navigateCalendar(-1), () => navigateCalendar(1), () => setEventFormOpen((open) => !open), () => setMatchFormOpen((open) => !open), () => setTrainingPlanOpen((open) => !open), (event) => void createEvent(event), (event) => void createMatch(event), (event) => void saveTrainingPlan(event), setEditingEvent, (item) => void cancelCalendarEvent(item), setEditingTraining, (item) => void restoreTrainingDate(item), (event) => void saveEvent(event), saveCalendarTrainingException, (matchId, attendance) => void setAttendance(matchId, attendance), () => setEditingEvent(null), () => setEditingTraining(null))}

          {tab === 'plantilla' && <section className="team-section"><div className="team-section-heading"><div><h2>Plantilla del equipo</h2><p>Jugadores asignados a {space.team.name}.</p></div><span className="team-member-count">{space.players.length} jugadores</span></div>
            {space.canManage && <form className="team-add-player" onSubmit={(event) => void addPlayer(event)}><div className="team-add-player-title"><span className="material-symbols-outlined" aria-hidden="true">person_add</span><div><strong>Añadir jugador</strong><small>Busca una cuenta con rol de Jugador o Cuerpo Técnico.</small></div></div><div className={`team-search-wrap${selectedPlayer ? ' has-selection' : ''}`} ref={candidatePickerRef}><label className="team-search"><span className="material-symbols-outlined" aria-hidden="true">search</span><input aria-label="Buscar jugador para añadir" aria-autocomplete="list" aria-expanded={candidateMenuOpen && query.trim().length >= 2 && !selectedPlayer} aria-controls="team-player-candidates" aria-activedescendant={candidateMenuOpen && candidates[activeCandidateIndex] ? `team-player-candidate-${candidates[activeCandidateIndex].userId}` : undefined} value={query} onFocus={() => { if (query.trim().length >= 2 && candidates.length > 0 && !selectedPlayer) setCandidateMenuOpen(true) }} onKeyDown={handleCandidateSearchKeyDown} onChange={(event) => searchPlayerCandidates(event.target.value)} placeholder="Nombre o correo electrónico" minLength={2}/></label>{selectedPlayer && <button className="team-selected-candidate" type="button" aria-label="Quitar jugador seleccionado" onClick={clearPlayerCandidate}><span>{selectedCandidate?.displayName}</span><span className="material-symbols-outlined" aria-hidden="true">close</span></button>}{candidateMenuOpen && query.trim().length >= 2 && !selectedPlayer && <div className="team-candidate-list" id="team-player-candidates" role="listbox" aria-label="Jugadores encontrados" aria-busy={searchingCandidates}>{searchingCandidates ? <div className="team-candidate-empty" role="status">Buscando jugadores…</div> : candidates.length > 0 ? candidates.map((candidate, index) => <button type="button" role="option" aria-selected={index === activeCandidateIndex} id={`team-player-candidate-${candidate.userId}`} key={candidate.userId} className={activeCandidateIndex === index ? 'active' : ''} onMouseEnter={() => setActiveCandidateIndex(index)} onClick={() => selectCandidate(candidate)}><span className="team-candidate-avatar">{candidate.displayName.slice(0, 1).toUpperCase()}</span><span><strong>{candidate.displayName}</strong><small>{candidate.email}</small></span><span className="material-symbols-outlined" aria-hidden="true">add_circle</span></button>) : <div className="team-candidate-empty" role="status">No hay jugadores disponibles con esa búsqueda.</div>}</div>}</div><div className="team-add-player-controls"><label><span>Dorsal</span><input type="number" min="0" max="99" value={jerseyNumber} onChange={(event) => setJerseyNumber(event.target.value)} placeholder="—"/></label><label><span>Posición</span><select value={position} onChange={(event) => setPosition(event.target.value)}>{positions.map((item) => <option key={item}>{item}</option>)}</select></label><button className="team-primary-button" type="submit" disabled={busy || !selectedPlayer}>{busy ? 'Añadiendo…' : 'Añadir a plantilla'}</button></div></form>}
            {space.players.length === 0 ? <div className="team-empty-inline">Todavía no hay jugadores asignados a este equipo.</div> : <div className="team-player-grid">{space.players.map((player) => <article className="team-player-card" key={player.userId}><div className="team-player-number">{player.jerseyNumber ?? '—'}</div><div className="team-player-details"><div className="team-player-name-row"><strong>{player.displayName}</strong><span className="team-player-position">{player.position}</span></div><small>{player.matchesPlayed} partidos · {player.minutesPlayed} min · {player.goals} goles{player.saves ? ` · ${player.saves} paradas` : ''}</small></div>{space.canManage && <button type="button" className="team-icon-button team-remove-player" aria-label={`Quitar a ${player.displayName} del equipo`} title="Retirar del equipo" onClick={() => void removePlayer(player)}><span className="material-symbols-outlined" aria-hidden="true">person_remove</span></button>}</article>)}</div>}
          </section>}

          {tab === 'pizarra' && <section className="team-section"><div className="team-section-heading"><div><h2>Pizarra del equipo</h2><p>Mensajes, instrucciones y material compartido por el cuerpo técnico.</p></div><span className="team-private-badge"><span className="material-symbols-outlined" aria-hidden="true">lock</span> Privada</span></div>{space.canManage && <form className="team-board-composer" onSubmit={(event) => void createBoardPost(event)}><label><span className="sr-only">Mensaje para la pizarra</span><textarea rows={4} maxLength={4000} value={boardText} onChange={(event) => setBoardText(event.target.value)} placeholder="Comparte las indicaciones del próximo entrenamiento o partido…"/></label><div className="team-board-composer-bottom"><label className="team-file-button"><span className="material-symbols-outlined" aria-hidden="true">attach_file</span>Vídeo o archivo<input type="file" accept="image/jpeg,image/png,image/webp,video/mp4,video/webm,application/pdf" multiple onChange={(event) => setBoardFiles(Array.from(event.target.files ?? []))}/></label><small>{boardFiles.length ? `${boardFiles.length} archivo(s) seleccionado(s)` : 'Imágenes, vídeos o PDF · hasta 4 archivos'}</small><button className="team-primary-button" disabled={busy || (!boardText.trim() && boardFiles.length === 0)}>{busy ? 'Publicando…' : 'Publicar mensaje'}</button></div></form>}{space.boardPosts.length === 0 ? <div className="team-empty-inline">Todavía no hay mensajes en la pizarra.</div> : <div className="team-board-list">{space.boardPosts.map((post) => <article className="team-board-post" key={post.id}><header><span className="team-author-avatar">{post.authorName.slice(0, 1).toUpperCase()}</span><div><strong>{post.authorName}</strong><small>{formatDateTime(post.createdAt)}</small></div></header>{post.content && <p>{post.content}</p>}{post.attachments.length > 0 && <div className="team-board-attachments">{post.attachments.map((file) => <BoardAttachmentPreview key={file.id} file={file}/>)}</div>}</article>)}</div>}</section>}

          {tab === 'estadisticas' && <section className="team-section"><div className="team-section-heading"><div><h2>Estadísticas de la plantilla</h2><p>Rendimiento deportivo acumulado de la temporada. No incluye datos médicos.</p></div></div><div className="team-stats-wrap"><table className="team-stats-table"><thead><tr><th>Jugador</th><th>Dorsal</th><th>Posición</th><th>Partidos</th><th>Minutos</th><th>Goles</th><th>Paradas</th>{space.canManage && <th>Editar</th>}</tr></thead><tbody>{space.players.map((player) => <tr key={player.userId}><td><strong>{player.displayName}</strong></td><td>#{player.jerseyNumber ?? '—'}</td><td>{player.position}</td><td>{space.canManage ? <input aria-label={`Partidos de ${player.displayName}`} form={`stats-${player.userId}`} name="matchesPlayed" type="number" min="0" defaultValue={player.matchesPlayed}/> : player.matchesPlayed}</td><td>{space.canManage ? <input aria-label={`Minutos de ${player.displayName}`} form={`stats-${player.userId}`} name="minutesPlayed" type="number" min="0" defaultValue={player.minutesPlayed}/> : player.minutesPlayed}</td><td>{space.canManage ? <input aria-label={`Goles de ${player.displayName}`} form={`stats-${player.userId}`} name="goals" type="number" min="0" defaultValue={player.goals}/> : player.goals}</td><td>{space.canManage ? <input aria-label={`Paradas de ${player.displayName}`} form={`stats-${player.userId}`} name="saves" type="number" min="0" defaultValue={player.saves}/> : player.saves}</td>{space.canManage && <td><form id={`stats-${player.userId}`} onSubmit={(event) => { event.preventDefault(); void updatePlayerStats(player, event.currentTarget) }}><button type="submit" className="team-icon-button" aria-label={`Guardar estadísticas de ${player.displayName}`}><span className="material-symbols-outlined" aria-hidden="true">save</span></button></form></td>}</tr>)}</tbody></table></div>{space.players.length === 0 && <div className="team-empty-inline">Las estadísticas aparecerán al añadir jugadores a la plantilla.</div>}</section>}
        </>}
      </>}
    </div>
  </main>
}
