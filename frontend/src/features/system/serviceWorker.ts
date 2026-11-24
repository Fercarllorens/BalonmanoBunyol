export async function configureServiceWorker(isDevelopment: boolean): Promise<void> {
  if (!('serviceWorker' in navigator)) return

  try {
    const registrations = await navigator.serviceWorker.getRegistrations()

    if (isDevelopment) {
      await Promise.all(registrations.map((registration) => registration.unregister()))
      if ('caches' in window) {
        const cacheNames = await caches.keys()
        await Promise.all(cacheNames.filter((name) => name.startsWith('cb-bunol-')).map((name) => caches.delete(name)))
      }
      return
    }

    const registration = await navigator.serviceWorker.register('/sw.js', { updateViaCache: 'none' })
    await registration.update()
  } catch (error) {
    console.error('No se pudo actualizar el service worker.', error)
  }
}
