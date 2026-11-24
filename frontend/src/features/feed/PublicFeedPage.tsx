import { type FormEvent, useCallback, useEffect, useMemo, useRef, useState } from 'react'
import SiteHeader, { type SessionUser } from '../system/SiteHeader'
import { type Theme } from '../system/ThemeToggle'
import { getCsrfToken } from '../auth/authApi'
import './feed.css'

type Attachment = { id: string; fileName: string; contentType: string; length: number; url: string }
type FeedPost = { id: string; authorId: string; authorDisplayName: string; authorAvatarUrl?: string | null; authorProfile: string; content: string; createdAt: string; attachments: Attachment[] }
type FeedPage = { posts: FeedPost[]; nextBefore: string | null; nextBeforeId: string | null; hasMore: boolean }
type FeedTab = 'general' | 'personal'
type FeedFilter = 'all' | 'official'
type SessionUserWithEmail = SessionUser & { email: string }

function formatRelativeTime(date: string) {
  const minutes = Math.max(0, Math.floor((Date.now() - new Date(date).getTime()) / 60000))
  if (minutes < 1) return 'Ahora'
  if (minutes < 60) return `Hace ${minutes} min`
  const hours = Math.floor(minutes / 60)
  if (hours < 24) return `Hace ${hours} h`
  return new Date(date).toLocaleDateString('es-ES', { day: 'numeric', month: 'short' })
}

function formatBytes(bytes: number) {
  return bytes >= 1024 * 1024 ? `${(bytes / 1024 / 1024).toFixed(1)} MB` : `${Math.ceil(bytes / 1024)} KB`
}

function ClubSidebar({ user }: { user: SessionUserWithEmail | null }) {
  const initials = user?.displayName.split(/\s+/).slice(0, 2).map((part) => part[0]).join('').toUpperCase() ?? 'CB'
  return <aside className="feed-rail">
    <section className="rail-profile-card">
      <div className="rail-profile-cover"><span>CB</span></div>
      <div className="rail-profile-body">
        <span className="rail-profile-avatar">{user?.avatarUrl ? <img src={user.avatarUrl} alt=""/> : initials}</span>
        <strong>{user?.displayName ?? 'Club Balonmano Buñol'}</strong>
        <small>{user?.email ?? 'Comunidad del club'}</small>
        <div className="rail-profile-stats">
          <span><b>{user ? 'Activa' : 'Pública'}</b> Cuenta</span>
          <span><b>{user?.roles.includes('Administrador') ? 'Admin' : user ? 'Socio' : 'Visitante'}</b> Perfil</span>
        </div>
      </div>
    </section>
    <section className="rail-widget rail-links">
      <div className="rail-widget-heading"><strong>Club Directo</strong></div>
      <a href="#galeria"><span className="material-symbols-outlined">photo_library</span><span>Galería oficial</span><span className="material-symbols-outlined rail-link-arrow">chevron_right</span></a>
      <a href="#categorias"><span className="material-symbols-outlined">groups</span><span>Nuestros equipos</span><span className="material-symbols-outlined rail-link-arrow">chevron_right</span></a>
      <a href="#patrocinadores"><span className="material-symbols-outlined">emoji_events</span><span>Patrocinadores</span><span className="material-symbols-outlined rail-link-arrow">chevron_right</span></a>
    </section>
    <section className="rail-sponsor-card">
      <span className="rail-sponsor-kicker"><span className="material-symbols-outlined">verified</span> Patrocinador oficial · Demo</span>
      <strong>Cooperativa Agrícola Buñol</strong>
      <p>Impulsando el deporte base y el balonmano de nuestra comarca.</p>
      <span className="rail-sponsor-tag">#OrgulloBuñol</span>
    </section>
  </aside>
}

function CompetitionSidebar() {
  const standings = [['1', 'BM Quart', '26'], ['2', 'C.B. Buñol', '24'], ['3', 'BM Mislata', '22'], ['4', 'C.BM. Torrent', '20']]
  return <aside className="feed-rail feed-rail-right">
    <section className="rail-widget rail-match-card">
      <div className="rail-widget-heading"><span>Próxima jornada</span><span className="mock-badge">DEMO</span></div>
      <div className="rail-match-teams">
        <div><span className="mock-team-logo">CB</span><strong>C.B. Buñol</strong></div>
        <span className="rail-match-versus">VS</span>
        <div><span className="mock-team-logo rival">MIS</span><strong>BM. Mislata</strong></div>
      </div>
      <div className="rail-match-details"><span className="material-symbols-outlined">event</span><span>Sábado · 18:30 h</span></div>
      <div className="rail-match-details"><span className="material-symbols-outlined">location_on</span><span>Pabellón Municipal · Buñol</span></div>
      <small className="mock-note">Encuentro de ejemplo, no oficial</small>
    </section>
    <section className="rail-widget rail-standings-card">
      <div className="rail-widget-heading"><strong>Clasificación</strong><span className="mock-badge">DEMO</span></div>
      <p className="mock-subtitle">1ª Autonómica · Grupo Centro</p>
      <div className="rail-standing-head"><span>POS · EQUIPO</span><span>PT</span></div>
      {standings.map(([position, team, points]) => <div className={`mock-standing ${team === 'C.B. Buñol' ? 'highlight' : ''}`} key={position}>
        <b>{position}</b><span>{team}</span><strong>{points}</strong>
      </div>)}
      <small className="mock-note">Clasificación de demostración</small>
    </section>
  </aside>
}

export default function PublicFeedPage({ theme, onToggleTheme, user }: { theme: Theme; onToggleTheme: () => void; user: SessionUser | null }) {
  const [activeTab, setActiveTab] = useState<FeedTab>('general')
  const [filter, setFilter] = useState<FeedFilter>('all')
  const [posts, setPosts] = useState<FeedPost[]>([])
  const [content, setContent] = useState('')
  const [files, setFiles] = useState<File[]>([])
  const [loading, setLoading] = useState(true)
  const [loadingMore, setLoadingMore] = useState(false)
  const [submitting, setSubmitting] = useState(false)
  const [hasMore, setHasMore] = useState(false)
  const [cursor, setCursor] = useState<{ before: string; beforeId: string } | null>(null)
  const [error, setError] = useState('')
  const [notice, setNotice] = useState('')
  const fileInput = useRef<HTMLInputElement>(null)
  const previews = useMemo(() => files.map((file) => ({ file, url: file.type.startsWith('image/') ? URL.createObjectURL(file) : '' })), [files])

  useEffect(() => () => previews.forEach((preview) => { if (preview.url) URL.revokeObjectURL(preview.url) }), [previews])

  const feedUrl = useCallback((nextCursor?: { before: string; beforeId: string } | null) => {
    const endpoint = activeTab === 'personal' ? '/api/v1/users/me/posts' : filter === 'official' ? '/api/v1/feed/announcements' : '/api/v1/feed'
    const url = new URL(endpoint, window.location.origin)
    url.searchParams.set('pageSize', '10')
    if (nextCursor) {
      url.searchParams.set('before', nextCursor.before)
      url.searchParams.set('beforeId', nextCursor.beforeId)
    }
    return url.toString()
  }, [activeTab, filter])

  const loadFeed = useCallback(async (append: boolean, nextCursor?: { before: string; beforeId: string } | null) => {
    if (append) setLoadingMore(true)
    else setLoading(true)
    setError('')
    try {
      const response = await fetch(feedUrl(nextCursor), { credentials: 'include', cache: 'no-store' })
      if (!response.ok) throw new Error('No se pudo cargar el feed. Inténtalo de nuevo.')
      const page = await response.json() as FeedPage
      setPosts((current) => append ? [...current, ...page.posts] : page.posts)
      setHasMore(page.hasMore)
      setCursor(page.hasMore && page.nextBefore && page.nextBeforeId ? { before: page.nextBefore, beforeId: page.nextBeforeId } : null)
    } catch (loadError) {
      setError(loadError instanceof Error ? loadError.message : 'Error al cargar las publicaciones.')
    } finally {
      setLoading(false)
      setLoadingMore(false)
    }
  }, [feedUrl])

  useEffect(() => {
    const timer = window.setTimeout(() => {
      setPosts([])
      setCursor(null)
      setHasMore(false)
      void loadFeed(false)
    }, 0)
    return () => window.clearTimeout(timer)
  }, [activeTab, filter, loadFeed])

  const selectFiles = (selected: FileList | null) => {
    if (!selected) return
    if (!user) {
      setError('Inicia sesión para adjuntar archivos.')
      return
    }
    setError('')
    const additions = Array.from(selected)
    const next = [...files, ...additions]
    if (next.length > 4) {
      setError('Puedes adjuntar un máximo de cuatro archivos por publicación.')
      return
    }
    for (const file of additions) {
      const isImage = ['image/jpeg', 'image/png', 'image/webp'].includes(file.type)
      const isVideo = ['video/mp4', 'video/webm'].includes(file.type)
      if (!isImage && !isVideo) {
        setError('Formatos admitidos: JPEG, PNG, WebP, MP4 y WebM.')
        return
      }
      if ((isImage && file.size > 10 * 1024 * 1024) || (isVideo && file.size > 50 * 1024 * 1024)) {
        setError(isImage ? 'Cada imagen debe pesar 10 MB o menos.' : 'Cada vídeo debe pesar 50 MB o menos.')
        return
      }
    }
    setFiles(next)
    if (fileInput.current) fileInput.current.value = ''
  }

  const publish = async (event: FormEvent<HTMLFormElement>) => {
    event.preventDefault()
    setError('')
    setNotice('')
    if (!user) {
      window.location.assign('/auth?tab=login')
      return
    }
    if (!content.trim() && files.length === 0) {
      setError('Escribe un mensaje o adjunta una imagen o vídeo.')
      return
    }
    setSubmitting(true)
    try {
      const token = await getCsrfToken()
      const formData = new FormData()
      formData.append('content', content)
      files.forEach((file) => formData.append('files', file))
      const response = await fetch('/api/v1/feed', { method: 'POST', credentials: 'include', cache: 'no-store', headers: { 'X-CSRF-TOKEN': token }, body: formData })
      const body = await response.json().catch(() => undefined)
      if (!response.ok) throw new Error(body?.message ?? 'No se pudo publicar el contenido.')
      setContent('')
      setFiles([])
      setNotice('Tu publicación se ha compartido en el feed general.')
      await loadFeed(false)
    } catch (publishError) {
      setError(publishError instanceof Error ? publishError.message : 'No se pudo publicar el contenido.')
    } finally { setSubmitting(false) }
  }

  const deletePost = async (postId: string) => {
    if (!user || !window.confirm('¿Quieres borrar esta publicación?')) return
    setError('')
    try {
      const token = await getCsrfToken()
      const response = await fetch(`/api/v1/feed/${postId}`, { method: 'DELETE', credentials: 'include', cache: 'no-store', headers: { 'X-CSRF-TOKEN': token } })
      if (!response.ok) throw new Error('No se pudo borrar la publicación.')
      setPosts((current) => current.filter((post) => post.id !== postId))
      setNotice('La publicación se ha borrado.')
    } catch (deleteError) {
      setError(deleteError instanceof Error ? deleteError.message : 'No se pudo borrar la publicación.')
    }
  }

  const displayUser = user as SessionUserWithEmail | null

  return <main className="public-feed-page"><SiteHeader theme={theme} onToggleTheme={onToggleTheme} user={user}/><div className="feed-shell">
    <div className="club-ticker"><span className="ticker-badge"><i/> AVISO CLUB</span><p><strong>Autobús de animación:</strong> Plazas abiertas para el próximo desplazamiento del Sénior Femenino.</p><span className="ticker-mock">Información de ejemplo</span></div>
    <div className="feed-layout"><ClubSidebar user={displayUser}/><section className="feed-center"><div className="feed-page-title"><div><span className="section-kicker">COMUNIDAD · CLUB BALONMANO BUÑOL</span><h1>Feed del Club</h1><p>La actualidad y los momentos de nuestra comunidad.</p></div></div>
      <div className="feed-tabs"><button className={activeTab === 'general' ? 'selected' : ''} onClick={() => setActiveTab('general')}><span className="material-symbols-outlined">groups</span> Feed general</button><button className={activeTab === 'personal' ? 'selected' : ''} onClick={() => setActiveTab('personal')}><span className="material-symbols-outlined">person</span> Mi feed</button><span className="feed-tabs-spacer"/><button className={filter === 'all' ? 'feed-filter selected-filter' : 'feed-filter'} onClick={() => setFilter('all')}>Todas</button><button className={filter === 'official' ? 'feed-filter selected-filter' : 'feed-filter'} onClick={() => setFilter('official')}>Comunicados</button></div>
      {activeTab === 'personal' ? <div className="feed-personal-panel"><span className="material-symbols-outlined">person_search</span><div><strong>Tu feed personal</strong><p>Las publicaciones que crees aparecerán aquí. La personalización por equipos y personas llegará más adelante.</p></div></div> : <>
      {user ? <form className="post-composer" onSubmit={publish}><div className="composer-top"><span className="composer-avatar">{user.avatarUrl ? <img src={user.avatarUrl} alt=""/> : user.displayName.slice(0, 1).toUpperCase()}</span><label className="composer-field"><span className="sr-only">Texto de la publicación</span><textarea maxLength={5000} rows={3} value={content} onChange={(event) => setContent(event.target.value)} placeholder="¿Qué quieres compartir con la familia del C.B. Buñol?"/></label></div>{files.length > 0 && <div className="composer-files">{previews.map(({ file, url }, index) => <div className="composer-file" key={`${file.name}-${index}`}>{url ? <img src={url} alt="Vista previa del adjunto"/> : <span className="material-symbols-outlined">videocam</span>}<span>{file.name} · {formatBytes(file.size)}</span><button type="button" aria-label={`Quitar ${file.name}`} onClick={() => setFiles((current) => current.filter((_, fileIndex) => fileIndex !== index))}>×</button></div>)}</div>}<div className="composer-bottom"><div className="composer-tools"><button type="button" onClick={() => fileInput.current?.click()}><span className="material-symbols-outlined">add_photo_alternate</span> Foto o vídeo</button><input ref={fileInput} hidden type="file" accept="image/jpeg,image/png,image/webp,video/mp4,video/webm" multiple onChange={(event) => selectFiles(event.target.files)}/><span className="composer-limit">Hasta 4 archivos · imágenes 10 MB · vídeos 50 MB</span></div><button className="publish-button" disabled={submitting}>{submitting ? 'Publicando…' : 'Publicar'} <span className="material-symbols-outlined">send</span></button></div>{error && <p className="feed-error" role="alert">{error}</p>}{notice && <p className="feed-notice" role="status">{notice}</p>}</form> : <div className="feed-signin-prompt">Inicia sesión para publicar en la comunidad. <a href="/auth?tab=login">Iniciar sesión</a></div>}
        {error && !user && <p className="feed-error" role="alert">{error}</p>}
      {loading ? <div className="feed-loading">Cargando publicaciones…</div> : posts.length === 0 ? <div className="feed-placeholder"><span className="material-symbols-outlined">dynamic_feed</span><div><strong>{filter === 'official' ? 'No hay comunicados todavía' : 'Todavía no hay publicaciones'}</strong><p>Sé la primera persona en compartir algo con el club.</p></div></div> : <div className="post-list">{posts.map((post) => <article className="feed-post" key={post.id}><header className="post-head"><span className={`post-avatar ${post.authorProfile === 'Administrador' ? 'official-avatar' : ''}`}>{post.authorAvatarUrl ? <img src={post.authorAvatarUrl} alt="" loading="lazy"/> : post.authorDisplayName.slice(0, 1).toUpperCase()}</span><div className="post-author"><strong>{post.authorDisplayName}{post.authorProfile === 'Administrador' && <span className="verified-mark">✓</span>}</strong><span>{post.authorProfile} · {formatRelativeTime(post.createdAt)}</span></div>{user?.id === post.authorId && <button className="delete-post" title="Borrar publicación" aria-label="Borrar publicación" onClick={() => void deletePost(post.id)}><span className="material-symbols-outlined">delete</span></button>}</header>{post.content && <p className="post-content">{post.content}</p>}{post.attachments.length > 0 && <div className={`post-media media-count-${Math.min(post.attachments.length, 4)}`}>{post.attachments.map((attachment) => attachment.contentType.startsWith('image/') ? <img key={attachment.id} src={attachment.url} alt={attachment.fileName} loading="lazy"/> : <video key={attachment.id} controls preload="metadata"><source src={attachment.url} type={attachment.contentType}/></video>)}</div>}<footer className="post-footer"><span><span className="material-symbols-outlined">favorite</span> Comunidad del club</span><span>{post.attachments.length > 0 ? `${post.attachments.length} adjunto${post.attachments.length > 1 ? 's' : ''}` : ''}</span></footer></article>)}</div>}
        {hasMore && cursor && <button className="load-more" disabled={loadingMore} onClick={() => void loadFeed(true, cursor)}>{loadingMore ? 'Cargando…' : 'Cargar más publicaciones'}</button>}
      </>}
    </section><CompetitionSidebar/></div>
  </div></main>
}
