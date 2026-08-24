import { Navigate, Outlet, Route, Routes } from 'react-router-dom'
import { useAuth } from '@/lib/auth'
import AppShell from '@/components/AppShell'
import { EmptyState, Spinner } from '@/components/ui'
import LoginPage from '@/features/auth/LoginPage'
import DashboardPage from '@/features/dashboard/DashboardPage'
import StartupsPage from '@/features/startups/StartupsPage'
import StartupDetailPage from '@/features/startups/StartupDetailPage'
import ProgramsPage from '@/features/programs/ProgramsPage'
import ApprovalsPage from '@/features/approvals/ApprovalsPage'
import ApprovalDetailPage from '@/features/approvals/ApprovalDetailPage'
import PortalPage from '@/features/portal/PortalPage'
import UsersPage from '@/features/users/UsersPage'
import AuditPage from '@/features/audit/AuditPage'
import type { SessionPermissions } from '@/api/types'

export default function App() {
  return (
    <Routes>
      <Route path="/giris" element={<LoginPage />} />
      <Route element={<RequireAuth />}>
        <Route element={<AppShell />}>
          <Route path="/pano" element={<DashboardPage />} />
          <Route path="/girisimler" element={<StartupsPage />} />
          <Route path="/girisimler/:id" element={<StartupDetailPage />} />
          <Route path="/programlar" element={<ProgramsPage />} />
          <Route path="/onaylar" element={<ApprovalsPage />} />
          <Route path="/onaylar/:id" element={<ApprovalDetailPage />} />

          <Route element={<RequirePermission permission="mustSubmitForApproval" />}>
            <Route path="/portal" element={<PortalPage />} />
          </Route>

          <Route element={<RequirePermission permission="canManageUsers" />}>
            <Route path="/kullanicilar" element={<UsersPage />} />
            <Route path="/denetim" element={<AuditPage />} />
          </Route>
        </Route>
      </Route>
      {/* Açılış panoya gider: karar destek ekranı ilk görülen ekran olmalı. */}
      <Route path="*" element={<Navigate to="/pano" replace />} />
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

/**
 * Yetkisiz rotada boş sayfa yerine açıklama gösterir. Yönlendirme yapmıyor:
 * paylaşılan bir bağlantıya tıklayan kullanıcı "neden göremiyorum" cevabını
 * almalı, sessizce başka ekrana atılmamalı. Asıl kontrol sunucuda.
 */
function RequirePermission({ permission }: { permission: keyof SessionPermissions }) {
  const { session } = useAuth()

  if (!session?.permissions[permission]) {
    return (
      <EmptyState
        title="Bu ekranı görme yetkiniz yok"
        hint="Erişim gerekiyorsa sistem yöneticinizden rolünüzün güncellenmesini isteyin."
      />
    )
  }

  return <Outlet />
}
