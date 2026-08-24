import { NavLink, Outlet } from 'react-router-dom'
import { useAuth } from '@/lib/auth'
import { roleLabels } from '@/lib/labels'
import { usePendingCount } from '@/features/approvals/queries'
import { Badge, Button } from '@/components/ui'

/**
 * Menü rol bazlı kurulur. Bu bir güvenlik önlemi değil — yetki sunucuda
 * uygulanıyor — ama kullanıcıya her tıklamada 403 yedirmenin alternatifi.
 */
export default function AppShell() {
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
      <header className="sticky top-0 z-10 border-b border-slate-200 bg-white/90 backdrop-blur dark:border-slate-800 dark:bg-slate-950/90">
        <div className="mx-auto flex max-w-7xl flex-wrap items-center gap-x-6 gap-y-3 px-6 py-3">
          <div className="flex items-center gap-3">
            <span className="flex size-9 items-center justify-center rounded-lg bg-brand-900 text-sm font-bold text-white">
              T3
            </span>
            <div className="leading-tight">
              <p className="text-sm font-semibold text-brand-900 dark:text-brand-100">
                Girişim Ekosistemi
              </p>
              <p className="text-xs text-slate-500">Yönetim Sistemi</p>
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
                        ? 'bg-brand-50 text-brand-900 dark:bg-brand-950 dark:text-brand-100'
                        : 'text-slate-600 hover:bg-slate-100 dark:text-slate-300 dark:hover:bg-slate-800'
                    }`
                  }
                >
                  {link.label}
                  {link.badge ? (
                    <span className="rounded-full bg-accent-500 px-1.5 py-0.5 text-[11px] font-semibold text-white">
                      {link.badge}
                    </span>
                  ) : null}
                </NavLink>
              ))}
          </nav>

          {session ? (
            <div className="ml-auto flex items-center gap-3">
              <div className="text-right leading-tight">
                <p className="text-sm font-medium text-slate-900 dark:text-slate-100">
                  {session.fullName}
                </p>
                <p className="text-xs text-slate-500">
                  {session.startupName ?? session.email}
                </p>
              </div>
              <Badge tone="bg-accent-500/15 text-accent-600 dark:text-accent-400">
                {roleLabels[session.role]}
              </Badge>
              <Button variant="outline" onClick={logout}>
                Çıkış
              </Button>
            </div>
          ) : null}
        </div>
      </header>

      <main className="mx-auto max-w-7xl px-6 py-8">
        <Outlet />
      </main>
    </div>
  )
}
