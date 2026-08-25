import type { ReactNode } from 'react'
import { Link, NavLink, Outlet } from 'react-router-dom'
import { useAuth } from '@/lib/auth'
import { roleLabels } from '@/lib/labels'
import { usePendingCount } from '@/features/approvals/queries'
import { Badge, Button } from '@/components/ui'
import { LogoLockup } from '@/components/Logo'

/**
 * Menü rol bazlı kurulur. Bu bir güvenlik önlemi değil — yetki sunucuda
 * uygulanıyor — ama kullanıcıya her tıklamada 403 yedirmenin alternatifi.
 */
export default function AppShell({ children }: { children?: ReactNode }) {
  const { session, logout } = useAuth()
  const permissions = session?.permissions

  // Rozet yalnızca karar verebilenlerde anlamlı: girişim kullanıcısının
  // "bekleyen" sayısı kendi gönderdikleridir, uyarı değil bilgi.
  const { data: pendingCount } = usePendingCount(permissions?.canReviewApprovals ?? false)

  const links = [
    { to: '/pano', label: 'Pano', show: true },
    { to: '/girisimler', label: 'Girişimler', show: true },
    { to: '/programlar', label: 'Programlar', show: true },
    {
      to: '/portal',
      label: 'Girişim portalı',
      show: permissions?.mustSubmitForApproval ?? false,
    },
    {
      to: '/onaylar',
      label: permissions?.canReviewApprovals ? 'Onay kuyruğu' : 'Önerilerim',
      show: (permissions?.canReviewApprovals || permissions?.mustSubmitForApproval) ?? false,
      badge: pendingCount,
    },
    { to: '/kullanicilar', label: 'Kullanıcılar', show: permissions?.canManageUsers ?? false },
    { to: '/denetim', label: 'Denetim izi', show: permissions?.canManageUsers ?? false },
  ]

  return (
    <div className="min-h-screen">
      {/* Klavye ve ekran okuyucu kullanıcısı her sayfada menünün tamamını
          geçmek zorunda kalmasın. Görünmez duruyor, odaklanınca görünür. */}
      <a
        href="#icerik"
        className="sr-only focus:not-sr-only focus:absolute focus:left-4 focus:top-4 focus:z-20 focus:rounded-lg focus:bg-brand-500 focus:px-4 focus:py-2 focus:text-sm focus:font-medium focus:text-white"
      >
        İçeriğe atla
      </a>

      <header className="sticky top-0 z-10 border-b border-stone-200 bg-white/90 backdrop-blur dark:border-stone-800 dark:bg-stone-950/90">
        <div className="mx-auto flex max-w-7xl flex-wrap items-center gap-x-6 gap-y-3 px-4 py-3 sm:px-6">
          {/* Logo panoya götürüyor: her arayüzde sol üstteki marka ana ekrana
              dönüş düğmesidir ve kullanıcı önce oraya tıklıyor. */}
          <Link to="/pano" className="rounded-lg focus-visible:ring-2 focus-visible:ring-brand-500/60">
            <LogoLockup compact />
          </Link>

          <nav className="flex flex-wrap items-center gap-1">
            {links
              .filter((link) => link.show)
              .map((link) => (
                <NavLink
                  key={link.to}
                  to={link.to}
                  className={({ isActive }) =>
                    `flex items-center gap-1.5 rounded-lg px-3 py-1.5 text-sm font-medium transition-colors ${
                      isActive
                        ? 'bg-brand-50 text-brand-700 dark:bg-brand-950 dark:text-brand-300'
                        : 'text-stone-600 hover:bg-brand-50 hover:text-brand-700 dark:text-stone-300 dark:hover:bg-stone-800'
                    }`
                  }
                >
                  {link.label}
                  {link.badge ? (
                    <span className="rounded-full bg-brand-500 px-1.5 py-0.5 text-[11px] font-semibold text-white">
                      {link.badge}
                    </span>
                  ) : null}
                </NavLink>
              ))}
          </nav>

          {/* Kullanıcı bloğu sarabiliyor ve metin kırpılabiliyor: 360 px'te
              e-posta + rol rozeti + iki eylem tek satıra sığmıyor ve satır
              dışarı taşıyordu (min-content genişliği). */}
          {session ? (
            <div className="ml-auto flex min-w-0 flex-wrap items-center justify-end gap-x-3 gap-y-2">
              <div className="min-w-0 text-right leading-tight">
                <p className="text-sm font-medium text-stone-900 dark:text-stone-100">
                  {session.fullName}
                </p>
                <p className="truncate text-xs text-stone-500">
                  {session.startupName ?? session.email}
                </p>
              </div>
              <Badge tone="bg-brand-100 text-brand-800 dark:bg-brand-950 dark:text-brand-200">
                {roleLabels[session.role]}
              </Badge>
              <Link
                to="/sifre-degistir"
                className="text-xs text-stone-500 hover:text-brand-700 hover:underline dark:hover:text-brand-200"
              >
                Şifre değiştir
              </Link>
              <Button variant="outline" onClick={logout}>
                Çıkış
              </Button>
            </div>
          ) : null}
        </div>
      </header>

      <main id="icerik" className="mx-auto max-w-7xl px-4 py-8 sm:px-6">
        {/* `children` yalnızca rota ağacına girmeyen ekranlar için (404). */}
        {children ?? <Outlet />}
      </main>

      {/* Alt bilgi her ekranda: KVKK metinlerine erişim tek bir ekrana
          gömülemez, aydınlatma yükümlülüğü sürekli erişilebilirlik ister. */}
      <footer className="mt-8 border-t border-stone-200 dark:border-stone-800">
        <div className="mx-auto flex max-w-7xl flex-wrap items-center gap-x-4 gap-y-2 px-4 py-6 text-xs text-stone-500 sm:px-6">
          <span>T3 Vakfı · Girişim Ekosistemi Yönetim Sistemi · Sürüm 1.0</span>
          <Link
            to="/kvkk-aydinlatma"
            className="text-brand-700 hover:underline dark:text-brand-200"
          >
            KVKK aydınlatma metni
          </Link>
          <Link
            to="/kullanim-sartlari"
            className="text-brand-700 hover:underline dark:text-brand-200"
          >
            Kullanım şartları
          </Link>
          <Link to="/marka" className="text-brand-700 hover:underline dark:text-brand-200">
            Logo paketi
          </Link>
          <a
            href="mailto:kvkk@t3vakfi.org.tr"
            className="text-brand-700 hover:underline dark:text-brand-200"
          >
            Destek ve KVKK başvurusu
          </a>
        </div>
      </footer>
    </div>
  )
}
