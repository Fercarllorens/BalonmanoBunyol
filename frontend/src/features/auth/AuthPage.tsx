import { type FormEvent, useState } from 'react'
import { type Theme } from '../system/ThemeToggle'
import SiteHeader, { type HeaderVariant, type SessionUser } from '../system/SiteHeader'
import { postJson } from './authApi'
import './auth.css'

type AuthUser = { id: string; email: string; displayName: string; roles: string[] }
type AuthTab = 'login' | 'register'
type AccountType = 'Socio' | 'Jugador' | 'CuerpoTecnico'

const icons = { ball: 'sports_handball', group: 'groups', history: 'history_edu', teams: 'sports', quote: 'format_quote', lock: 'lock', account: 'account_circle', eye: 'visibility', eyeOff: 'visibility_off', arrow: 'arrow_forward', support: 'contact_support', check: 'check_circle' }
function Icon({ name, className = '' }: { name: string; className?: string }) { return <span className={`material-symbols-outlined ${className}`} aria-hidden="true">{name}</span> }

export default function AuthPage({ theme, onToggleTheme, onAuthenticated, headerVariant = 'standard' }: { theme: Theme; onToggleTheme: () => void; onAuthenticated: (user: SessionUser) => void; headerVariant?: HeaderVariant }) {
  const [tab, setTab] = useState<AuthTab>(new URLSearchParams(window.location.search).get('tab') === 'register' ? 'register' : 'login')
  const [accountType, setAccountType] = useState<AccountType>('Socio')
  const [showPassword, setShowPassword] = useState(false)
  const [remember, setRemember] = useState(true)
  const [busy, setBusy] = useState(false)
  const [error, setError] = useState('')

  const submitLogin = async (event: FormEvent<HTMLFormElement>) => {
    event.preventDefault(); setBusy(true); setError('')
    const form = new FormData(event.currentTarget)
    try {
      const result = await postJson<AuthUser>('/api/v1/auth/login', { email: form.get('email'), password: form.get('password'), rememberMe: remember })
      onAuthenticated(result)
    } catch (submitError) { setError(submitError instanceof Error ? submitError.message : 'No se pudo iniciar sesión.') }
    finally { setBusy(false) }
  }

  const submitRegister = async (event: FormEvent<HTMLFormElement>) => {
    event.preventDefault(); setBusy(true); setError('')
    const form = new FormData(event.currentTarget)
    try {
      const result = await postJson<AuthUser>('/api/v1/auth/register', {
        firstName: form.get('firstName'), lastName: form.get('lastName'), email: form.get('email'), password: form.get('password'), accountType,
      })
      onAuthenticated(result)
    } catch (submitError) { setError(submitError instanceof Error ? submitError.message : 'No se pudo completar el registro.') }
    finally { setBusy(false) }
  }

  return <main className="auth-page"><SiteHeader variant={headerVariant} theme={theme} onToggleTheme={onToggleTheme} user={null}/><div className="auth-layout"><section className="auth-brand-panel"><div className="auth-brand-content"><a className="auth-brand" href="/"><img className="auth-brand-shield" src="/club-bunol-escudo.jpeg" alt=""/><span><small>CLUB OFICIAL</small><strong>CLUB BALONMANO BUÑOL</strong></span></a><span className="auth-official"><i/> PABELLÓN MUNICIPAL EL PLANELL · TEMPORADA OFICIAL</span><h1>Pasión, Cantera y Deporte en <em>El Planell</em></h1><p>El portal integral para jugadores, cuerpo técnico, directiva y afición del C.B. Buñol. Vive el pulso deportivo de nuestro club en un solo lugar.</p><div className="auth-metrics"><article><Icon name={icons.group}/><strong>+250</strong><small>Socios activos</small></article><article><Icon name={icons.history}/><strong>1982</strong><small>Fundación oficial</small></article><article><Icon name={icons.teams}/><strong>9</strong><small>Equipos federados</small></article></div><blockquote><Icon name={icons.quote}/><div><p>“Más de cuatro décadas defendiendo los colores de Buñol con entrega, respeto y espíritu de equipo en cada partido.”</p><strong>Junta Directiva &amp; Afición <i>·</i> #OrgulloBuñol</strong></div></blockquote></div><footer className="auth-sponsors"><span>COLABORADORES Y PATROCINIO LOCAL</span><div><b>CAJA BUÑOL</b><i>·</i><b>CERÁMICAS LA HOYA</b><i>·</i><b>DEPORTES TURIA</b></div></footer></section>

    <section className="auth-form-panel"><div className="auth-card"><div className="auth-card-heading"><div className="auth-card-brand"><img src="/club-bunol-escudo.jpeg" alt=""/><div><strong>Acceso Federativo</strong><small>Temporada Oficial 2024 – 2025</small></div></div><span className="auth-secure"><Icon name={icons.lock}/> Sesión segura</span></div>
      <div className="auth-tabs" role="tablist" aria-label="Acceso o registro"><button type="button" role="tab" aria-selected={tab === 'login'} className={tab === 'login' ? 'active' : ''} onClick={() => { setTab('login'); setError('') }}>Iniciar sesión</button><button type="button" role="tab" aria-selected={tab === 'register'} className={tab === 'register' ? 'active' : ''} onClick={() => { setTab('register'); setError('') }}>Crear cuenta</button></div>

      {tab === 'login' ? (
        <form className="auth-form" onSubmit={submitLogin}>
          <div className="auth-form-intro"><span className="auth-kicker">BIENVENIDO DE NUEVO</span><h2>Accede al portal del club</h2><p>Introduce tus credenciales para continuar.</p></div>
          <label className="auth-field"><span>Correo electrónico</span><div><Icon name="account_circle"/><input name="email" type="email" autoComplete="email" placeholder="nombre@correo.com" required/></div></label>
          <label className="auth-field"><span>Contraseña</span><div><Icon name="lock"/><input name="password" type={showPassword ? 'text' : 'password'} autoComplete="current-password" placeholder="Introduce tu contraseña" required/><button type="button" className="password-toggle" aria-label={showPassword ? 'Ocultar contraseña' : 'Mostrar contraseña'} onClick={() => setShowPassword(!showPassword)}><Icon name={showPassword ? icons.eyeOff : icons.eye}/></button></div></label>
          <div className="auth-form-options"><label><input type="checkbox" checked={remember} onChange={(event) => setRemember(event.target.checked)}/> Mantener sesión iniciada</label><span>¿Olvidaste la contraseña? <small>Próximamente</small></span></div>
          {error && <div className="auth-alert error-alert" role="alert">{error}</div>}
          <button className="auth-submit" disabled={busy}>{busy ? 'Comprobando…' : 'Acceder al portal del club'} <Icon name={icons.arrow}/></button>
          <p className="auth-switch-copy">¿Aún no tienes cuenta? <button type="button" onClick={() => setTab('register')}>Regístrate</button></p>
        </form>
      ) : (
        <form className="auth-form register-form" onSubmit={submitRegister}>
          <div className="auth-form-intro"><span className="auth-kicker">ÚNETE A LA FAMILIA</span><h2>Crea tu cuenta</h2><p>Forma parte de la comunidad del C.B. Buñol.</p></div>
          <div className="auth-name-fields"><label className="auth-field"><span>Nombre</span><div><input name="firstName" autoComplete="given-name" placeholder="Tu nombre" required/></div></label><label className="auth-field"><span>Apellidos</span><div><input name="lastName" autoComplete="family-name" placeholder="Tus apellidos" required/></div></label></div>
          <label className="auth-field"><span>Correo electrónico</span><div><Icon name="account_circle"/><input name="email" type="email" autoComplete="email" placeholder="nombre@correo.com" required/></div></label>
          <label className="auth-field"><span>Contraseña</span><div><Icon name="lock"/><input name="password" type={showPassword ? 'text' : 'password'} autoComplete="new-password" placeholder="Mínimo 12 caracteres" minLength={12} required/><button type="button" className="password-toggle" aria-label={showPassword ? 'Ocultar contraseña' : 'Mostrar contraseña'} onClick={() => setShowPassword(!showPassword)}><Icon name={showPassword ? icons.eyeOff : icons.eye}/></button></div></label>
          <fieldset className="account-type"><legend>¿Cómo quieres participar?</legend><div className="account-type-grid"><button type="button" className={accountType === 'Socio' ? 'selected' : ''} onClick={() => setAccountType('Socio')}><Icon name="loyalty"/><strong>Socio / Afición</strong><small>Acceso básico inmediato</small></button><button type="button" className={accountType === 'Jugador' ? 'selected' : ''} onClick={() => setAccountType('Jugador')}><Icon name="sports_handball"/><strong>Jugador/a</strong><small>Rol sujeto a validación</small></button><button type="button" className={accountType === 'CuerpoTecnico' ? 'selected' : ''} onClick={() => setAccountType('CuerpoTecnico')}><Icon name="assignment_ind"/><strong>Cuerpo Técnico</strong><small>Rol sujeto a validación</small></button></div><p>{accountType === 'Socio' ? 'Tu cuenta quedará activa como Socio/Afición al completar el registro.' : 'Tu cuenta básica quedará activa como Socio/Afición. Administración revisará tu solicitud de rol.'}</p></fieldset>
          {error && <div className="auth-alert error-alert" role="alert">{error}</div>}
          <button className="auth-submit" disabled={busy}>{busy ? 'Creando cuenta…' : 'Crear cuenta'} <Icon name={icons.arrow}/></button>
          <p className="auth-switch-copy">¿Ya tienes cuenta? <button type="button" onClick={() => setTab('login')}>Inicia sesión</button></p>
        </form>
      )}
      <div className="auth-help"><span><Icon name={icons.support}/> ¿Necesitas ayuda con tu cuenta?</span><a href="mailto:contacto@balonmanobunol.es">Contactar con secretaría ↗</a></div>
    </div></section></div></main>
}
