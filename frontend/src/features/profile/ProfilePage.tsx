import { type ChangeEvent, type FormEvent, useEffect, useRef, useState } from 'react'
import SiteHeader, { type SessionUser } from '../system/SiteHeader'
import { type Theme } from '../system/ThemeToggle'
import { deleteRequest, postForm, putJson } from '../auth/authApi'

type Profile = SessionUser & {
  firstName: string
  lastName: string
  preferredTeamId: string | null
  avatarUrl: string | null
  availableTeams: { id: string; name: string; category: string }[]
}
const maxAvatarSize = 5 * 1024 * 1024
const acceptedAvatarTypes = ['image/jpeg', 'image/png', 'image/webp']

export default function ProfilePage({ theme, onToggleTheme, user, onProfileUpdated }: {
  theme: Theme
  onToggleTheme: () => void
  user: SessionUser
  onProfileUpdated: (user: SessionUser) => void
}) {
  const [profile, setProfile] = useState<Profile | null>(null)
  const [loading, setLoading] = useState(true)
  const [saving, setSaving] = useState(false)
  const [uploading, setUploading] = useState(false)
  const [error, setError] = useState('')
  const [notice, setNotice] = useState('')
  const [avatarPreview, setAvatarPreview] = useState('')
  const fileInput = useRef<HTMLInputElement>(null)

  useEffect(() => {
    let active = true
    void fetch('/api/v1/profile/', { credentials: 'include', cache: 'no-store' })
      .then(async (response) => {
        if (!response.ok) throw new Error('No se pudo cargar tu perfil.')
        const result = await response.json() as Profile
        if (active) setProfile(result)
      })
      .catch((loadError: unknown) => { if (active) setError(loadError instanceof Error ? loadError.message : 'No se pudo cargar tu perfil.') })
      .finally(() => { if (active) setLoading(false) })
    return () => { active = false }
  }, [])

  useEffect(() => () => { if (avatarPreview) URL.revokeObjectURL(avatarPreview) }, [avatarPreview])

  const updateSession = (updated: Profile) => {
    setProfile(updated)
    onProfileUpdated({ ...user, ...updated })
  }

  const selectAvatar = (event: ChangeEvent<HTMLInputElement>) => {
    const file = event.target.files?.[0]
    if (!file) return
    setError('')
    setNotice('')
    if (!acceptedAvatarTypes.includes(file.type)) {
      setError('Formatos admitidos: JPEG, PNG y WebP.')
      event.target.value = ''
      return
    }
    if (file.size > maxAvatarSize) {
      setError('La imagen debe pesar 5 MB o menos.')
      event.target.value = ''
      return
    }
    setAvatarPreview(URL.createObjectURL(file))
    if (profile) void uploadAvatar(file)
    event.target.value = ''
  }

  const uploadAvatar = async (file: File) => {
    setUploading(true)
    setError('')
    setNotice('')
    try {
      const formData = new FormData()
      formData.append('avatar', file)
      const updated = await postForm<Profile>('/api/v1/profile/avatar', formData)
      updateSession(updated)
      setAvatarPreview('')
      setNotice('Tu foto de perfil se ha actualizado.')
    } catch (uploadError) {
      setError(uploadError instanceof Error ? uploadError.message : 'No se pudo guardar la foto de perfil.')
      setAvatarPreview('')
    } finally { setUploading(false) }
  }

  const removeAvatar = async () => {
    setError('')
    setNotice('')
    try {
      await deleteRequest('/api/v1/profile/avatar')
      if (profile) updateSession({ ...profile, avatarUrl: null })
      setAvatarPreview('')
      setNotice('Se ha eliminado tu foto de perfil.')
    } catch (removeError) {
      setError(removeError instanceof Error ? removeError.message : 'No se pudo eliminar la foto de perfil.')
    }
  }

  const saveProfile = async (event: FormEvent<HTMLFormElement>) => {
    event.preventDefault()
    if (!profile) return
    setSaving(true)
    setError('')
    setNotice('')
    const form = new FormData(event.currentTarget)
    try {
      const updated = await putJson<Profile>('/api/v1/profile/', {
        firstName: form.get('firstName'),
        lastName: form.get('lastName'),
        email: form.get('email'),
        preferredTeamId: form.get('preferredTeamId') || null,
      })
      updateSession(updated)
      setNotice('Tu perfil se ha guardado correctamente.')
    } catch (saveError) {
      setError(saveError instanceof Error ? saveError.message : 'No se pudo guardar tu perfil.')
    } finally { setSaving(false) }
  }

  const avatarSrc = avatarPreview || profile?.avatarUrl || ''
  const initials = (profile?.displayName || user.displayName).split(/\s+/).slice(0, 2).map((part) => part[0]).join('').toUpperCase()

  return <main className="profile-page">
    <SiteHeader theme={theme} onToggleTheme={onToggleTheme} user={user}/>
    <section className="profile-content">
      <a className="profile-back-link" href="/feed"><span className="material-symbols-outlined" aria-hidden="true">arrow_back</span>Volver al Feed</a>
      <header className="profile-heading"><span className="section-kicker">TU CUENTA · CLUB BALONMANO BUÑOL</span><h1>Personalizar perfil</h1><p>Actualiza tus datos, tu foto y el equipo que prefieres seguir.</p></header>
      {error && <div className="profile-message profile-error" role="alert">{error}</div>}
      {notice && <div className="profile-message profile-success" role="status">{notice}</div>}
      {loading ? <p className="profile-loading" role="status">Cargando perfil…</p> : profile && <div className="profile-grid">
        <section className="profile-card profile-avatar-card" aria-labelledby="avatar-heading">
          <div className="profile-card-heading"><span className="material-symbols-outlined" aria-hidden="true">photo_camera</span><div><h2 id="avatar-heading">Foto de perfil</h2><p>Elige una imagen para tu cuenta.</p></div></div>
          <div className="profile-avatar-preview">{avatarSrc ? <img src={avatarSrc} alt="Vista previa de tu avatar"/> : <span>{initials || <span className="material-symbols-outlined" aria-hidden="true">person</span>}</span>}</div>
          <div className="profile-avatar-actions"><button type="button" className="profile-secondary-button" onClick={() => fileInput.current?.click()} disabled={uploading}>{uploading ? 'Subiendo…' : 'Elegir foto'}</button>{profile.avatarUrl && <button type="button" className="profile-text-button" onClick={() => void removeAvatar()}>Eliminar</button>}</div>
          <input ref={fileInput} className="profile-file-input" type="file" accept="image/jpeg,image/png,image/webp" aria-label="Elegir foto de perfil" onChange={selectAvatar}/>
          <small className="profile-hint">JPEG, PNG o WebP · máximo 5 MB</small>
        </section>
        <form className="profile-card profile-form" onSubmit={saveProfile}>
          <div className="profile-card-heading"><span className="material-symbols-outlined" aria-hidden="true">badge</span><div><h2>Información personal</h2><p>Estos datos identifican tu cuenta en la comunidad.</p></div></div>
          <div className="profile-fields">
            <label><span>Nombre</span><input name="firstName" autoComplete="given-name" defaultValue={profile.firstName} maxLength={80} required/></label>
            <label><span>Apellidos</span><input name="lastName" autoComplete="family-name" defaultValue={profile.lastName} maxLength={79} required/></label>
            <label className="profile-field-wide"><span>Correo electrónico</span><input name="email" type="email" autoComplete="email" defaultValue={profile.email} maxLength={256} required/><small>También se utiliza para iniciar sesión.</small></label>
            <label className="profile-field-wide"><span>Equipo preferido</span><select aria-label="Equipo preferido" name="preferredTeamId" value={profile.preferredTeamId ?? ''} onChange={(event) => setProfile((current) => current ? { ...current, preferredTeamId: event.target.value || null } : current)}><option value="">Sin preferencia</option>{profile.availableTeams.map((team) => <option key={team.id} value={team.id}>{team.name} · {team.category}</option>)}</select><small>Personaliza las novedades del equipo que quieres seguir.</small></label>
          </div>
          <div className="profile-form-footer"><button className="profile-save-button" type="submit" disabled={saving}>{saving ? 'Guardando…' : 'Guardar cambios'}</button></div>
        </form>
      </div>}
    </section>
  </main>
}
