import { StrictMode } from 'react'
import { createRoot } from 'react-dom/client'
import './index.css'
import App from './App.tsx'
import { configureServiceWorker } from './features/system/serviceWorker'

window.addEventListener('load', () => { void configureServiceWorker(import.meta.env.DEV) })

createRoot(document.getElementById('root')!).render(
  <StrictMode>
    <App />
  </StrictMode>,
)
