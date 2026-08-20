import { NavLink, Outlet } from 'react-router-dom'
import { useAuth } from '@/lib/auth'
import { roleLabels } from '@/lib/labels'
import { Badge, Button } from '@/components/ui'

export default function AppShell() {
  const { session, logout } = useAuth()

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

          <nav className="flex items-center gap-1">
            <NavLink
              to="/girisimler"
              className={({ isActive }) =>
                `rounded-lg px-3 py-1.5 text-sm font-medium transition-colors ${
                  isActive
                    ? 'bg-brand-50 text-brand-900 dark:bg-brand-950 dark:text-brand-100'
                    : 'text-slate-600 hover:bg-slate-100 dark:text-slate-300 dark:hover:bg-slate-800'
                }`
              }
            >
              Girişimler
            </NavLink>
            <NavLink
              to="/programlar"
              className={({ isActive }) =>
                `rounded-lg px-3 py-1.5 text-sm font-medium transition-colors ${
                  isActive
                    ? 'bg-brand-50 text-brand-900 dark:bg-brand-950 dark:text-brand-100'
                    : 'text-slate-600 hover:bg-slate-100 dark:text-slate-300 dark:hover:bg-slate-800'
                }`
              }
            >
              Programlar
            </NavLink>
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
