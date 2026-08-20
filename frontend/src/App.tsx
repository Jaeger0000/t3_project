import { Navigate, Outlet, Route, Routes } from 'react-router-dom'
import { useAuth } from '@/lib/auth'
import AppShell from '@/components/AppShell'
import { Spinner } from '@/components/ui'
import LoginPage from '@/features/auth/LoginPage'
import StartupsPage from '@/features/startups/StartupsPage'
import StartupDetailPage from '@/features/startups/StartupDetailPage'
import ProgramsPage from '@/features/programs/ProgramsPage'

export default function App() {
  return (
    <Routes>
      <Route path="/giris" element={<LoginPage />} />
      <Route element={<RequireAuth />}>
        <Route element={<AppShell />}>
          <Route path="/girisimler" element={<StartupsPage />} />
          <Route path="/girisimler/:id" element={<StartupDetailPage />} />
          <Route path="/programlar" element={<ProgramsPage />} />
        </Route>
      </Route>
      <Route path="*" element={<Navigate to="/girisimler" replace />} />
    </Routes>
  )
}

/**
 * Rota koruması. Jeton varken oturum doğrulanana kadar bekler; aksi halde
 * sayfa yenilendiğinde kullanıcı bir an giriş ekranına atılır.
 */
function RequireAuth() {
  const { session, isResolving } = useAuth()

  if (isResolving) {
    return (
      <div className="flex min-h-screen items-center justify-center">
        <Spinner label="Oturum doğrulanıyor…" />
      </div>
    )
  }

  if (!session) return <Navigate to="/giris" replace />

  return <Outlet />
}
