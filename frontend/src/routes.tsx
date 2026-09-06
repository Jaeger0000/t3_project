import { Route, createRoutesFromElements } from 'react-router-dom'
import AppShell from '@/components/AppShell'
import SuspenseLayout from '@/components/SuspenseLayout'
import {
  NotFoundRoute,
  PublicPage,
  RequireAuth,
  RequirePermission,
} from '@/components/RouteGuards'
import LandingPage from '@/features/landing/LandingPage'
import LoginPage from '@/features/auth/LoginPage'
import RegisterStartupPage from '@/features/auth/RegisterStartupPage'
import {
  ApprovalDetailPage,
  ApprovalsPage,
  AssistantChatPage,
  AuditPage,
  ChangePasswordPage,
  DashboardPage,
  ForgotPasswordPage,
  NotificationsPage,
  PortalPage,
  PrivacyNoticePage,
  ProfilePage,
  ProgramsPage,
  RegistrationRequestsPage,
  ResetPasswordPage,
  StartupDetailPage,
  StartupsPage,
  TermsPage,
  UsersPage,
} from '@/pages'

/**
 * Rota ağacı. JSX olarak yazılıp veri yönlendiricisine (`createBrowserRouter`,
 * bkz. main.tsx) veriliyor — okunabilirlik `<Routes>` ile aynı kalıyor ama
 * `useBlocker` gibi veri yönlendiricisine bağlı kancalar çalışıyor. Kaydedilmemiş
 * form uyarısı (bkz. useUnsavedChanges) bunu gerektiriyordu.
 */
export const appRoutes = createRoutesFromElements(
  <Route element={<SuspenseLayout />}>
    <Route path="/giris" element={<LoginPage />} />
    <Route path="/kayit-ol" element={<RegisterStartupPage />} />
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
        {/* Ayrı bir izin aranmıyor: herkes kendi yetkisi kadarını görüyor,
            süzme sunucuda yapılıyor (girişim kullanıcısı kendi kaydını sorar). */}
        <Route path="/asistan" element={<AssistantChatPage />} />
        <Route path="/bildirimler" element={<NotificationsPage />} />
        <Route path="/profil" element={<ProfilePage />} />
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
          <Route path="/kayit-basvurulari" element={<RegistrationRequestsPage />} />
        </Route>
      </Route>
    </Route>
    {/* Kök adres tanıtım sayfası. Oturumu olan kullanıcıyı sayfanın kendisi
        panoya yolluyor (bkz. LandingPage) — burada koşulsuz bir yönlendirme
        olsaydı siteye ilk gelen ziyaretçi doğrudan giriş formuna düşerdi. */}
    <Route path="/" element={<LandingPage />} />
    <Route path="*" element={<NotFoundRoute />} />
  </Route>,
)
