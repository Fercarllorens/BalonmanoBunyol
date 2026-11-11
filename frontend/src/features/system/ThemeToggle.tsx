export type Theme = 'light' | 'dark'

export function ThemeToggle({ theme, onToggle }: { theme: Theme; onToggle: () => void }) {
  const nextTheme = theme === 'light' ? 'oscuro' : 'claro'
  return (
    <button className="theme-toggle" type="button" onClick={onToggle} aria-label={`Cambiar a tema ${nextTheme}`} title={`Cambiar a tema ${nextTheme}`}>
      <span className="material-symbols-outlined" aria-hidden="true">{theme === 'light' ? 'dark_mode' : 'light_mode'}</span>
      <span className="theme-toggle-label">{theme === 'light' ? 'Modo oscuro' : 'Modo claro'}</span>
    </button>
  )
}
