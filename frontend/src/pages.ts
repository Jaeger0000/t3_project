import { lazy } from 'react'

/*
 * Ekranlar isteğe göre yükleniyor. Tek parça paket 400 kB'a çıkmıştı ve
 * içindeki en pahalı iki parça (grafik kütüphanesi ve girişim kartı) yalnızca
 * iki ekranda kullanılıyor: giriş ekranını görmek için ekosistem panosunun
 * grafiklerini indirmek gereksiz.
 *
 * Açılış (tanıtım) sayfası, giriş ekranı ve 404 bilinçli olarak statik: ilk
 * ikisi her ziyaretin ilk karesi, sonuncusu hata yolunda ek bir ağ isteğine
 * bağlanmamalı.
 */
export const ForgotPasswordPage = lazy(() => import('@/features/auth/ForgotPasswordPage'))
export const ResetPasswordPage = lazy(() => import('@/features/auth/ResetPasswordPage'))
export const ChangePasswordPage = lazy(() => import('@/features/auth/ChangePasswordPage'))
export const DashboardPage = lazy(() => import('@/features/dashboard/DashboardPage'))
export const StartupsPage = lazy(() => import('@/features/startups/StartupsPage'))
export const StartupDetailPage = lazy(() => import('@/features/startups/StartupDetailPage'))
export const ProgramsPage = lazy(() => import('@/features/programs/ProgramsPage'))
export const AssistantChatPage = lazy(() => import('@/features/assistant/AssistantChatPage'))
export const ApprovalsPage = lazy(() => import('@/features/approvals/ApprovalsPage'))
export const ApprovalDetailPage = lazy(() => import('@/features/approvals/ApprovalDetailPage'))
export const PortalPage = lazy(() => import('@/features/portal/PortalPage'))
export const UsersPage = lazy(() => import('@/features/users/UsersPage'))
export const RegistrationRequestsPage = lazy(
  () => import('@/features/registrations/RegistrationRequestsPage'),
)
export const AuditPage = lazy(() => import('@/features/audit/AuditPage'))
export const PrivacyNoticePage = lazy(() => import('@/features/legal/PrivacyNoticePage'))
export const TermsPage = lazy(() => import('@/features/legal/TermsPage'))

