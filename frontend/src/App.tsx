import type { ReactNode } from 'react'
import { Navigate, Outlet, Route, Routes, useLocation } from 'react-router-dom'
import { useAuth } from '@/lib/auth'
import AppShell from '@/components/AppShell'
import { Button, EmptyState, ErrorState, Spinner } from '@/components/ui'
import LoginPage from '@/features/auth/LoginPage'
import ForgotPasswordPage from '@/features/auth/ForgotPasswordPage'
import ResetPasswordPage from '@/features/auth/ResetPasswordPage'
import ChangePasswordPage from '@/features/auth/ChangePasswordPage'
import DashboardPage from '@/features/dashboard/DashboardPage'
import StartupsPage from '@/features/startups/StartupsPage'
import StartupDetailPage from '@/features/startups/StartupDetailPage'
import ProgramsPage from '@/features/programs/ProgramsPage'
import ApprovalsPage from '@/features/approvals/ApprovalsPage'
import ApprovalDetailPage from '@/features/approvals/ApprovalDetailPage'
import PortalPage from '@/features/portal/PortalPage'
import UsersPage from '@/features/users/UsersPage'
import AuditPage from '@/features/audit/AuditPage'
import NotFoundPage from '@/features/errors/NotFoundPage'
import PrivacyNoticePage from '@/features/legal/PrivacyNoticePage'
import TermsPage from '@/features/legal/TermsPage'
import type { SessionPermissions } from '@/api/types'

export default function App() {
  return (
    <Routes>
      <Route path="/giris" element={<LoginPage />} />
      <Route path="/sifremi-unuttum" element={<ForgotPasswordPage />} />
      <Route path="/sifre-sifirla/:token" element={<ResetPasswordPage />} />

      {/* KVKK metinleri giriş yapılmadan açılabilmeli: aydınlatma yükümlülüğü
          veri vermeden önce başlar. */}
      <Route path="/kvkk-aydinlatma" element={<PublicPage><PrivacyNoticePage /></PublicPage>} />
      <Route path="/kullanim-sartlari" element={<PublicPage><TermsPage /></PublicPage>} />

      <Route element={<RequireAuth />}>
        <Route element={<AppShell />}>
          <Route path="/pano" element={<DashboardPage />} />
          <Route path="/girisimler" element={<StartupsPage />} />
          <Route path="/girisimler/:id" element={<StartupDetailPage />} />
          <Route path="/programlar" element={<ProgramsPage />} />
          <Route path="/sifre-degistir" element={<ChangePasswordPage />} />

          {/* Onay ekranı iki yetkiden birini gerektiriyor: karar veren ya da
              öneri gönderen. Karar Verici rolünde ikisi de yok ve ekran
              sonsuza dek boş kalıyordu. */}
          <Route
            element={<RequirePermission anyOf={['canReviewApprovals', 'mustSubmitForApproval']} />}
          >
            <Route path="/onaylar" element={<ApprovalsPage />} />
            <Route path="/onaylar/:id" element={<ApprovalDetailPage />} />
          </Route>

          <Route element={<RequirePermission anyOf={['mustSubmitForApproval']} />}>
            <Route path="/portal" element={<PortalPage />} />
          </Route>

          <Route element={<RequirePermission anyOf={['canManageUsers']} />}>
            <Route path="/kullanicilar" element={<UsersPage />} />
            <Route path="/denetim" element={<AuditPage />} />
          </Route>
        </Route>
      </Route>
      {/* Açılış panoya gider: karar destek ekranı ilk görülen ekran olmalı. */}
      <Route path="/" element={<Navigate to="/pano" replace />} />
      <Route path="*" element={<NotFoundRoute />} />
    </Routes>
  )
}

/**
 * Oturum gerektirmeyen içerik sayfası. Oturum açıkken kabuk içinde gösteriliyor
 * (kullanıcı menüyü kaybetmesin), kapalıyken çıplak — kabuk oturum verisi
 * bekliyor.
 */
function PublicPage({ children }: { children: ReactNode }) {
  const { session, isResolving } = useAuth()

  if (isResolving) {
    return (
      <div className="flex min-h-screen items-center justify-center">
        <Spinner label="Oturum doğrulanıyor…" />
      </div>
    )
  }

  if (!session) {
    return <div className="mx-auto min-h-screen max-w-3xl px-4 py-12 sm:px-6">{children}</div>
  }

  return <AppShell>{children}</AppShell>
}

/** Bilinmeyen adres. Aynı kabuk kuralını izliyor. */
function NotFoundRoute() {
  return (
    <PublicPage>
      <NotFoundPage />
    </PublicPage>
  )
}

/**
 * Rota koruması. Jeton varken oturum doğrulanana kadar bekler; aksi halde
 * sayfa yenilendiğinde kullanıcı bir an giriş ekranına atılır.
 */
function RequireAuth() {
  const { session, isResolving, connectionError, retrySession } = useAuth()
  const location = useLocation()

  if (isResolving) {
    return (
      <div className="flex min-h-screen items-center justify-center">
        <Spinner label="Oturum doğrulanıyor…" />
      </div>
    )
  }

  // Sunucuya ulaşılamıyor: jeton hâlâ elimizde, kullanıcıyı giriş ekranına
  // atmak yanlış olurdu — sorun yetkide değil bağlantıda.
  if (connectionError) {
    return (
      <div className="mx-auto flex min-h-screen max-w-md flex-col justify-center gap-4 px-6">
        <ErrorState message={connectionError} />
        <Button onClick={retrySession}>Yeniden dene</Button>
      </div>
    )
  }

  if (!session) {
    // Kullanıcı gitmek istediği adrese giriş sonrası dönsün.
    return <Navigate to="/giris" replace state={{ from: location.pathname + location.search }} />
  }

  // Yönetici şifre attı: kullanıcı kendi şifresini belirlemeden başka ekrana
  // geçemiyor. Amaç şifrenin ikinci sahibini ortadan kaldırmak.
  if (session.mustChangePassword && location.pathname !== '/sifre-degistir') {
    return <Navigate to="/sifre-degistir" replace />
  }

  return <Outlet />
}

/**
 * Yetkisiz rotada boş sayfa yerine açıklama gösterir. Yönlendirme yapmıyor:
 * paylaşılan bir bağlantıya tıklayan kullanıcı "neden göremiyorum" cevabını
 * almalı, sessizce başka ekrana atılmamalı. Asıl kontrol sunucuda.
 */
function RequirePermission({ anyOf }: { anyOf: (keyof SessionPermissions)[] }) {
  const { session } = useAuth()
  const allowed = anyOf.some((permission) => session?.permissions[permission])

  if (!allowed) {
    return (
      <EmptyState
        title="Bu ekranı görme yetkiniz yok"
        hint="Erişim gerekiyorsa sistem yöneticinizden rolünüzün güncellenmesini isteyin."
      />
    )
  }

  return <Outlet />
}
