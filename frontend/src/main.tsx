import { StrictMode } from 'react'
import { createRoot } from 'react-dom/client'
import { RouterProvider, createBrowserRouter } from 'react-router-dom'
import { QueryClientProvider } from '@tanstack/react-query'
import { queryClient } from '@/lib/queryClient'
import { AuthProvider } from '@/lib/AuthProvider'
import ErrorBoundary from '@/components/ErrorBoundary'
import { appRoutes } from '@/routes'
import '@/index.css'

// Veri yönlendiricisi: `useBlocker` (kaydedilmemiş form uyarısı) yalnızca bu
// kurulumla çalışıyor. AuthProvider yönlendirici kancası kullanmadığı için
// dışta kalabiliyor.
const router = createBrowserRouter(appRoutes)

createRoot(document.getElementById('root')!).render(
  <StrictMode>
    {/* Sınır en dışta: sağlayıcıların kendisinde çıkan hata da yakalanmalı. */}
    <ErrorBoundary>
      <QueryClientProvider client={queryClient}>
        <AuthProvider>
          <RouterProvider router={router} />
        </AuthProvider>
      </QueryClientProvider>
    </ErrorBoundary>
  </StrictMode>,
)
