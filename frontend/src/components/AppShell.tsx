import { useState, type ReactNode } from 'react'
import { Link, NavLink, Outlet } from 'react-router-dom'
import { useAuth } from '@/lib/auth'
import { usePendingCount } from '@/features/approvals/queries'
import { useUnreadNotificationCount } from '@/features/notifications/queries'
import { Avatar, Button } from '@/components/ui'
import { LogoLockup } from '@/components/Logo'
import AssistantWidget from '@/features/assistant/AssistantWidget'

/**
 * Menü rol bazlı kurulur. Bu bir güvenlik önlemi değil — yetki sunucuda
 * uygulanıyor — ama kullanıcıya her tıklamada 403 yedirmenin alternatifi.
 *
 * Menü sol kenar çubuğunda: geniş rota listesi (Kullanıcılar, Kayıt
 * başvuruları, Denetim izi vb. SuperAdmin'de tek satıra sığmıyordu) yatayda
 * değil dikeyde büyür. Küçük ekranda çubuk katlanıyor, üstte hamburger duruyor.
 */
export default function AppShell({ children }: { children?: ReactNode }) {
  const { session, logout } = useAuth()
  const permissions = session?.permissions
  const [navOpen, setNavOpen] = useState(false)

  // Çıkış sonrası giriş ekranı değil tanıtım sayfası açılır: `logout('/')`
  // RequireAuth'a bunu söylüyor (bkz. auth.ts) — ayrı bir navigate() çağrısı
  // RequireAuth'un kendi yönlendirmesiyle yarışıp bazen /giris kazanıyordu.
  const handleLogout = () => logout('/')

  // Rozet yalnızca karar verebilenlerde anlamlı: girişim kullanıcısının
  // "bekleyen" sayısı kendi gönderdikleridir, uyarı değil bilgi.
  const { data: pendingCount } = usePendingCount(permissions?.canReviewApprovals ?? false)

  // Bildirimler menüsü iki farklı görünüm: girişim kullanıcısının kendi gelen
  // kutusu (rozetli) ve SuperAdmin'in tüm sistemin gözetim ekranı (rozetsiz —
  // "okunmadı" burada kişisel bir kavram değil). Program Yöneticisi ve Karar
  // Verici'ye menüde hiç görünmüyor; gönderme zaten girişim kartından yapılıyor.
  const isStartupUser = session?.role === 'StartupUser'
  const isSuperAdmin = session?.role === 'SuperAdmin'
  const { data: unreadNotifications } = useUnreadNotificationCount(isStartupUser)

  const links = [
    { to: '/pano', label: 'Pano', show: true },
    { to: '/girisimler', label: 'Girişimler', show: true },
    { to: '/programlar', label: 'Programlar', show: true },
    // Girişim kullanıcısına da açık: kendi kaydını sorabiliyor, kapsamı
    // sunucu daraltıyor.
    { to: '/asistan', label: 'AI asistan', show: true },
    {
      to: '/bildirimler',
      label: 'Bildirimler',
      show: isStartupUser || isSuperAdmin,
      badge: isStartupUser ? unreadNotifications : undefined,
    },
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
    {
      to: '/kayit-basvurulari',
      label: 'Kayıt başvuruları',
      show: permissions?.canManageUsers ?? false,
    },
    { to: '/denetim', label: 'Denetim izi', show: permissions?.canManageUsers ?? false },
  ]

  return (
    <div className="min-h-screen md:flex">
      {/* Klavye ve ekran okuyucu kullanıcısı her sayfada menünün tamamını
          geçmek zorunda kalmasın. Görünmez duruyor, odaklanınca görünür. */}
      <a
        href="#icerik"
        className="sr-only focus:not-sr-only focus:absolute focus:left-4 focus:top-4 focus:z-20 focus:rounded-lg focus:bg-brand-500 focus:px-4 focus:py-2 focus:text-sm focus:font-medium focus:text-white"
      >
        İçeriğe atla
      </a>

      {/* Küçük ekranda üst çubuk: logo + hamburger. Kenar çubuğu genişliği
          375 px'te tek başına sayfayı taşırıyordu, bu yüzden md altında
          gizli ve tıklanınca açılan bir panele dönüşüyor. */}
      <div className="flex items-center justify-between border-b border-stone-200 bg-white px-4 py-3 dark:border-stone-800 dark:bg-stone-950 md:hidden">
        <Link to="/pano" className="rounded-lg focus-visible:ring-2 focus-visible:ring-brand-500/60">
          <LogoLockup compact />
        </Link>
        <button
          type="button"
          onClick={() => setNavOpen((open) => !open)}
          aria-expanded={navOpen}
          aria-controls="kenar-menu"
          className="rounded-lg border border-stone-300 px-3 py-1.5 text-sm font-medium text-stone-700 dark:border-stone-700 dark:text-stone-200"
        >
          Menü
        </button>
      </div>

      <aside
        id="kenar-menu"
        className={`${navOpen ? 'flex' : 'hidden'} w-full shrink-0 flex-col border-b border-stone-200 bg-white dark:border-stone-800 dark:bg-stone-950 md:sticky md:top-0 md:flex md:h-screen md:w-64 md:border-b-0 md:border-r`}
      >
        <Link
          to="/pano"
          className="hidden rounded-lg p-5 focus-visible:ring-2 focus-visible:ring-brand-500/60 md:block"
        >
          <LogoLockup />
        </Link>

        <nav className="flex flex-col gap-1 overflow-y-auto p-3 md:flex-1">
          {links
            .filter((link) => link.show)
            .map((link) => (
              <NavLink
                key={link.to}
                to={link.to}
                onClick={() => setNavOpen(false)}
                className={({ isActive }) =>
                  `flex items-center justify-between gap-1.5 rounded-lg px-3 py-2 text-sm font-semibold transition-colors ${
                    isActive
                      ? 'bg-brand-50 text-brand-700 dark:bg-brand-950 dark:text-brand-300'
                      : 'text-stone-800 hover:bg-brand-100 hover:text-brand-800 dark:text-stone-100 dark:hover:bg-stone-700 dark:hover:text-brand-200'
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

        {/* Kullanıcı bloğu sade tutuluyor: rol/girişim gibi ayrıntılar artık
            yalnızca /profil sayfasında — burada sadece kimliğe gitmek için
            gereken en az bilgi (ad-soyad) duruyor. */}
        {session ? (
          <div className="flex flex-col gap-2 border-t border-stone-200 p-3 dark:border-stone-800">
            <Link
              to="/profil"
              onClick={() => setNavOpen(false)}
              className="flex min-w-0 items-center gap-3 rounded-lg px-3 py-2 hover:bg-brand-100 dark:hover:bg-stone-700"
            >
              <Avatar className="h-9 w-9" />
              <span className="flex min-w-0 flex-col gap-1">
                <span className="text-xs font-medium text-stone-500">Profil</span>
                <span className="truncate text-sm font-medium text-stone-900 dark:text-stone-100">
                  {session.fullName}
                </span>
              </span>
            </Link>
            <Button variant="outline" onClick={handleLogout} className="w-full">
              Çıkış
            </Button>
          </div>
        ) : null}
      </aside>

      <div className="flex min-w-0 flex-1 flex-col">
        <main id="icerik" className="mx-auto w-full max-w-7xl flex-1 px-4 py-8 sm:px-6">
          {/* `children` yalnızca rota ağacına girmeyen ekranlar için (404). */}
          {children ?? <Outlet />}
        </main>

        {/* Alt bilgi her ekranda: KVKK metinlerine erişim tek bir ekrana
            gömülemez, aydınlatma yükümlülüğü sürekli erişilebilirlik ister. */}
        <footer className="border-t border-stone-200 dark:border-stone-800">
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
            <a
              href="mailto:kvkk@t3vakfi.org.tr"
              className="text-brand-700 hover:underline dark:text-brand-200"
            >
              Destek ve KVKK başvurusu
            </a>
          </div>
        </footer>
      </div>

      {/* Sağ altta yüzen sohbet: soru, kullanıcının aklına geldiği ekranda
          sorulabilmeli. Kendi içinde oturum/rota kontrolü yapıyor. */}
      <AssistantWidget />
    </div>
  )
}
