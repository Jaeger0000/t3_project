import type { ReactNode } from 'react'
import { NavLink, Outlet } from 'react-router-dom'
import { useAuth } from '@/lib/auth'
import { roleLabels } from '@/lib/labels'
import { usePendingCount } from '@/features/approvals/queries'
import { Badge, Button } from '@/components/ui'

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
      <header className="sticky top-0 z-10 border-b border-stone-200 bg-white/90 backdrop-blur dark:border-stone-800 dark:bg-stone-950/90">
        <div className="mx-auto flex max-w-7xl flex-wrap items-center gap-x-6 gap-y-3 px-4 py-3 sm:px-6">
          <div className="flex items-center gap-3">
            <span className="flex size-9 items-center justify-center rounded-lg bg-brand-500 text-sm font-bold text-white">
              T3
            </span>
            <div className="leading-tight">
              <p className="text-sm font-semibold text-stone-900 dark:text-stone-50">
                Girişim Ekosistemi
              </p>
              <p className="text-xs text-stone-500">Yönetim Sistemi</p>
            </div>
          </div>

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

          {session ? (
            <div className="ml-auto flex items-center gap-3">
              <div className="text-right leading-tight">
                <p className="text-sm font-medium text-stone-900 dark:text-stone-100">
                  {session.fullName}
                </p>
                <p className="text-xs text-stone-500">
                  {session.startupName ?? session.email}
                </p>
              </div>
              <Badge tone="bg-brand-100 text-brand-800 dark:bg-brand-950 dark:text-brand-200">
                {roleLabels[session.role]}
              </Badge>
              <Button variant="outline" onClick={logout}>
                Çıkış
              </Button>
            </div>
          ) : null}
        </div>
      </header>

      <main className="mx-auto max-w-7xl px-4 py-8 sm:px-6">
        {/* `children` yalnızca rota ağacına girmeyen ekranlar için (404). */}
        {children ?? <Outlet />}
      </main>
    </div>
  )
}
