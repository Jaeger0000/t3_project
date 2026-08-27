import type { ReactNode } from 'react'
import { Navigate, Outlet, useLocation } from 'react-router-dom'
import { useAuth } from '@/lib/auth'
import AppShell from '@/components/AppShell'
import { Button, EmptyState, ErrorState, Spinner } from '@/components/ui'
import NotFoundPage from '@/features/errors/NotFoundPage'
import type { SessionPermissions } from '@/api/types'

/*
 * Rota koruyucuları rota ağacından ayrı dosyada (bkz. routes.tsx): tek dosya
 * hem bileşen hem bileşen-olmayan (rota dizisi) ihraç ettiğinde Vite'ın fast
 * refresh'i o dosya için devre dışı kalıyor.
 */

/**
 * Oturum gerektirmeyen içerik sayfası. Oturum açıkken kabuk içinde gösteriliyor
 * (kullanıcı menüyü kaybetmesin), kapalıyken çıplak — kabuk oturum verisi
 * bekliyor.
 */
export function PublicPage({ children }: { children: ReactNode }) {
  const { session, isResolving } = useAuth()

  if (isResolving) {
    return (
      <div className="flex min-h-screen items-center justify-center">
        <Spinner label="Oturum doğrulanıyor…" />
      </div>
    )
  }

  if (!session) {
    // Metin sayfaları okunabilir bir kolonda duruyor.
    return (
      <div className="mx-auto min-h-screen max-w-3xl px-4 py-12 sm:px-6">{children}</div>
    )
  }

  return <AppShell>{children}</AppShell>
}

/** Bilinmeyen adres. Aynı kabuk kuralını izliyor. */
export function NotFoundRoute() {
  return (
    <PublicPage>
      <NotFoundPage />
    </PublicPage>
  )
}

/**
 * Rota koruması. Oturum sunucudan doğrulanana kadar bekler; aksi halde sayfa
 * yenilendiğinde kullanıcı bir an giriş ekranına atılır. Kimlik artık çerezde
 * olduğu için "elimde jeton var mı" diye bakamıyoruz, cevabı /api/me veriyor.
 */
export function RequireAuth() {
  const { session, isResolving, connectionError, retrySession } = useAuth()
  const location = useLocation()

  if (isResolving) {
    return (
      <div className="flex min-h-screen items-center justify-center">
        <Spinner label="Oturum doğrulanıyor…" />
      </div>
    )
  }

  // Sunucuya ulaşılamıyor: oturum çerezi yerinde, kullanıcıyı giriş ekranına
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
export function RequirePermission({ anyOf }: { anyOf: (keyof SessionPermissions)[] }) {
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
