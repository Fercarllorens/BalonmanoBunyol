import { afterEach, describe, expect, it, vi } from 'vitest'
import { configureServiceWorker } from './serviceWorker'

describe('configureServiceWorker', () => {
  afterEach(() => vi.unstubAllGlobals())

  it('desregistra workers y elimina cachés del club en desarrollo', async () => {
    const unregister = vi.fn().mockResolvedValue(true)
    const registration = { unregister }
    const getRegistrations = vi.fn().mockResolvedValue([registration])
    const deleteCache = vi.fn().mockResolvedValue(true)
    vi.stubGlobal('navigator', { serviceWorker: { getRegistrations } })
    const cacheStorage = { keys: vi.fn().mockResolvedValue(['cb-bunol-shell-v2', 'other-cache']), delete: deleteCache }
    vi.stubGlobal('window', { caches: cacheStorage })
    vi.stubGlobal('caches', cacheStorage)

    await configureServiceWorker(true)

    expect(unregister).toHaveBeenCalledOnce()
    expect(deleteCache).toHaveBeenCalledOnce()
    expect(deleteCache).toHaveBeenCalledWith('cb-bunol-shell-v2')
  })

  it('comprueba el worker de producción sin caché HTTP del script', async () => {
    const update = vi.fn().mockResolvedValue(undefined)
    const register = vi.fn().mockResolvedValue({ update })
    vi.stubGlobal('navigator', { serviceWorker: { getRegistrations: vi.fn().mockResolvedValue([]), register } })

    await configureServiceWorker(false)

    expect(register).toHaveBeenCalledWith('/sw.js', { updateViaCache: 'none' })
    expect(update).toHaveBeenCalledOnce()
  })
})
