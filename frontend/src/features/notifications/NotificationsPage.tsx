import { useMemo, useState } from 'react'
import { useAuth } from '@/lib/auth'
import { formatDateTime } from '@/lib/format'
import { Badge, Button, Card, EmptyState, ErrorState, Spinner } from '@/components/ui'
import { useDocumentTitle } from '@/lib/useDocumentTitle'
import {
  useDeleteNotification,
  useMarkNotificationRead,
  useMarkNotificationsRead,
  useMyNotifications,
  useRestoreNotification,
  useSentNotifications,
} from './queries'
import type { NotificationRow } from '@/api/types'

/**
 * Bildirimler ekranı role göre iki farklı görünüme ayrılır, ikisi de
 * Gmail tarzı filtre düğmeleriyle TEK bir listeyi süzüyor:
 * - Girişim kullanıcısı: kendi gelen kutusu — Okunmamışlar / Okunmuşlar /
 *   Silinenler.
 * - SuperAdmin: tüm sistemin gözetim ekranı — Gelenler (Program
 *   Yöneticilerinin gönderdikleri) / Gönderilenler (kendi gönderdikleri) /
 *   Okunmamışlar / Okunmuşlar / Silinenler.
 */
export default function NotificationsPage() {
  useDocumentTitle('Bildirimler')
  const { session } = useAuth()

  return session?.role === 'SuperAdmin' ? <SuperAdminNotifications /> : <MyNotifications />
}

type MyFilter = 'unread' | 'read' | 'deleted'

function MyNotifications() {
  const [filter, setFilter] = useState<MyFilter>('unread')
  const notifications = useMyNotifications()
  const markAllRead = useMarkNotificationsRead()
  const markOneRead = useMarkNotificationRead()
  const deleteOne = useDeleteNotification()
  const restoreOne = useRestoreNotification()

  if (notifications.isPending) return <Spinner label="Bildirimler yükleniyor…" />
  if (notifications.error) {
    return <ErrorState message={notifications.error.message} error={notifications.error} />
  }

  const items = notifications.data?.items ?? []
  const active = items.filter((item) => item.deletedByRecipientAt === null)
  const unreadItems = active.filter((item) => item.readAt === null)
  const readItems = active.filter((item) => item.readAt !== null)
  const deletedItems = items.filter((item) => item.deletedByRecipientAt !== null)

  const visible =
    filter === 'unread' ? unreadItems : filter === 'read' ? readItems : deletedItems

  const emptyCopy: Record<MyFilter, { title: string; hint: string }> = {
    unread: {
      title: 'Bekleyen bildiriminiz yok',
      hint: 'Yönetici size bir bildirim gönderdiğinde burada ve e-posta kutunuzda görünür.',
    },
    read: { title: 'Okunmuş bildiriminiz yok', hint: 'Okundu işaretlediğiniz bildirimler burada birikir.' },
    deleted: { title: 'Silinmiş bildiriminiz yok', hint: 'Sildiğiniz bildirimler burada durur, geri yükleyebilirsiniz.' },
  }

  return (
    <div className="flex flex-col gap-6">
      <header>
        <h1 className="text-2xl font-bold text-stone-900 dark:text-stone-50">Bildirimlerim</h1>
        <p className="mt-1 text-sm text-stone-500">
          Program yöneticinizden ya da sistem yöneticisinden gelen bildirimler.
        </p>
      </header>

      <div className="flex flex-wrap items-center justify-between gap-3">
        <div className="flex flex-wrap gap-2">
          <Button variant={filter === 'unread' ? 'primary' : 'outline'} onClick={() => setFilter('unread')}>
            Okunmamışlar{unreadItems.length > 0 ? ` (${unreadItems.length})` : ''}
          </Button>
          <Button variant={filter === 'read' ? 'primary' : 'outline'} onClick={() => setFilter('read')}>
            Okunmuşlar
          </Button>
          <Button variant={filter === 'deleted' ? 'primary' : 'outline'} onClick={() => setFilter('deleted')}>
            Silinenler
          </Button>
        </div>

        {filter === 'unread' && unreadItems.length > 0 ? (
          <Button
            variant="ghost"
            disabled={markAllRead.isPending}
            onClick={() => markAllRead.mutate()}
          >
            {markAllRead.isPending ? 'İşaretleniyor…' : 'Tümünü okundu işaretle'}
          </Button>
        ) : null}
      </div>

      {visible.length === 0 ? (
        <EmptyState title={emptyCopy[filter].title} hint={emptyCopy[filter].hint} />
      ) : (
        <div className="flex flex-col gap-3">
          {visible.map((item) =>
            filter === 'deleted' ? (
              <NotificationCard
                key={item.id}
                notification={item}
                onRestore={() => restoreOne.mutate(item.id)}
                restoring={restoreOne.isPending && restoreOne.variables === item.id}
              />
            ) : (
              <NotificationCard
                key={item.id}
                notification={item}
                onMarkRead={() => markOneRead.mutate(item.id)}
                onDelete={() => deleteOne.mutate(item.id)}
                markingRead={markOneRead.isPending && markOneRead.variables === item.id}
                deleting={deleteOne.isPending && deleteOne.variables === item.id}
              />
            ),
          )}
        </div>
      )}
    </div>
  )
}

type AdminFilter = 'inbox' | 'sent' | 'unread' | 'read' | 'deleted'

function SuperAdminNotifications() {
  const [filter, setFilter] = useState<AdminFilter>('inbox')
  const sent = useSentNotifications(true)
  const markOneRead = useMarkNotificationRead()
  const deleteOne = useDeleteNotification()

  const all = useMemo(() => {
    if (!sent.data) return []
    return [...sent.data.directFromSuperAdmin, ...sent.data.fromProgramManagers].sort(
      (a, b) => new Date(b.sentAt).getTime() - new Date(a.sentAt).getTime(),
    )
  }, [sent.data])

  if (sent.isPending) return <Spinner label="Bildirimler yükleniyor…" />
  if (sent.error) return <ErrorState message={sent.error.message} error={sent.error} />

  const data = sent.data!
  const active = all.filter((item) => item.deletedByRecipientAt === null)
  const inboxItems = data.fromProgramManagers.filter((item) => item.deletedByRecipientAt === null)
  const sentItems = data.directFromSuperAdmin.filter((item) => item.deletedByRecipientAt === null)
  const unreadItems = active.filter((item) => item.readAt === null)
  const readItems = active.filter((item) => item.readAt !== null)
  const deletedItems = all.filter((item) => item.deletedByRecipientAt !== null)

  const visible =
    filter === 'inbox' ? inboxItems
    : filter === 'sent' ? sentItems
    : filter === 'unread' ? unreadItems
    : filter === 'read' ? readItems
    : deletedItems

  const emptyCopy: Record<AdminFilter, { title: string; hint: string }> = {
    inbox: { title: 'Program Yöneticisi bildirimi yok', hint: 'Bir Program Yöneticisi bir girişime bildirim gönderdiğinde burada görünür.' },
    sent: { title: 'Doğrudan bir bildirim göndermediniz', hint: 'Girişim kartındaki "Bildirim Gönder" düğmesiyle gönderdikleriniz burada birikir.' },
    unread: { title: 'Okunmamış bildirim yok', hint: 'Girişimlerin henüz okumadığı bildirimler burada görünür.' },
    read: { title: 'Okunmuş bildirim yok', hint: '' },
    deleted: { title: 'Silinmiş bildirim yok', hint: 'Bir girişimin kendi kutusundan sildiği bildirimler burada görünür.' },
  }

  // SuperAdmin'in "sil" düğmesi kaydı gerçekten kaldırıyor (bkz.
  // DeleteNotificationHandler) — alıcının kendi kutusundaki gibi geri
  // alınabilir bir gizleme değil, bu yüzden burada "geri yükle" yok.
  return (
    <div className="flex flex-col gap-6">
      <header>
        <h1 className="text-2xl font-bold text-stone-900 dark:text-stone-50">Bildirimler</h1>
        <p className="mt-1 text-sm text-stone-500">
          Girişimlere giden tüm bildirimler — kim gönderdiğine göre ayrılmış, birbirine
          karışmasın diye.
        </p>
      </header>

      <div className="flex flex-wrap gap-2">
        <Button variant={filter === 'inbox' ? 'primary' : 'outline'} onClick={() => setFilter('inbox')}>
          Gelenler{inboxItems.length > 0 ? ` (${inboxItems.length})` : ''}
        </Button>
        <Button variant={filter === 'sent' ? 'primary' : 'outline'} onClick={() => setFilter('sent')}>
          Gönderilenler{sentItems.length > 0 ? ` (${sentItems.length})` : ''}
        </Button>
        <Button variant={filter === 'unread' ? 'primary' : 'outline'} onClick={() => setFilter('unread')}>
          Okunmamışlar{unreadItems.length > 0 ? ` (${unreadItems.length})` : ''}
        </Button>
        <Button variant={filter === 'read' ? 'primary' : 'outline'} onClick={() => setFilter('read')}>
          Okunmuşlar
        </Button>
        <Button variant={filter === 'deleted' ? 'primary' : 'outline'} onClick={() => setFilter('deleted')}>
          Silinenler{deletedItems.length > 0 ? ` (${deletedItems.length})` : ''}
        </Button>
      </div>

      {visible.length === 0 ? (
        <EmptyState title={emptyCopy[filter].title} hint={emptyCopy[filter].hint} />
      ) : (
        <div className="flex flex-col gap-3">
          {visible.map((item) => (
            <NotificationCard
              key={item.id}
              notification={item}
              showRecipient
              onMarkRead={() => markOneRead.mutate(item.id)}
              onDelete={() => deleteOne.mutate(item.id)}
              markingRead={markOneRead.isPending && markOneRead.variables === item.id}
              deleting={deleteOne.isPending && deleteOne.variables === item.id}
            />
          ))}
        </div>
      )}
    </div>
  )
}

/**
 * `showRecipient`: SuperAdmin gözetim ekranında hangi girişime gittiği de
 * önemli; kendi gelen kutumda (StartupUser) zaten bu bilgi örtük (hep kendi
 * girişimi), tekrar göstermek gürültü olurdu.
 *
 * Aksiyon düğmeleri (mark-read/delete/restore) çağıran bileşenden hangi
 * callback'lerin geçtiğine göre görünür — kart kendi başına "hangi sekmedeyim"
 * bilmiyor, yalnızca kendisine verilen düğmeleri çiziyor.
 */
function NotificationCard({
  notification,
  showRecipient = false,
  onMarkRead,
  onDelete,
  onRestore,
  markingRead = false,
  deleting = false,
  restoring = false,
}: {
  notification: NotificationRow
  showRecipient?: boolean
  onMarkRead?: () => void
  onDelete?: () => void
  onRestore?: () => void
  markingRead?: boolean
  deleting?: boolean
  restoring?: boolean
}) {
  const unread = notification.readAt === null

  return (
    <Card className={`p-4 ${unread ? 'border-brand-300 dark:border-brand-800' : ''}`}>
      <div className="flex flex-wrap items-center justify-between gap-2">
        <div className="flex flex-wrap items-center gap-2 text-sm">
          <span className="font-medium text-stone-900 dark:text-stone-100">
            {notification.sentByName}
          </span>
          <Badge tone="bg-brand-100 text-brand-800 dark:bg-brand-950 dark:text-brand-200">
            {notification.sentByRoleLabel}
          </Badge>
          {showRecipient ? (
            <span className="text-stone-500">→ {notification.startupName}</span>
          ) : null}
        </div>
        <span className="text-xs text-stone-500">{formatDateTime(notification.sentAt)}</span>
      </div>

      <p className="mt-2 text-sm whitespace-pre-line text-stone-700 dark:text-stone-300">
        {notification.message}
      </p>

      <div className="mt-3 flex flex-wrap items-center justify-between gap-3">
        <div className="flex flex-wrap items-center gap-3 text-xs text-stone-500">
          {notification.deletedByRecipientAt ? (
            <Badge tone="bg-stone-200 text-stone-700 dark:bg-stone-800 dark:text-stone-300">
              Silindi: {formatDateTime(notification.deletedByRecipientAt)}
            </Badge>
          ) : unread ? (
            <Badge tone="bg-brand-500 text-white">Yeni</Badge>
          ) : notification.readAt ? (
            <span>Okundu: {formatDateTime(notification.readAt)}</span>
          ) : null}
          <span>{notification.emailSent ? '✉️ E-posta gönderildi' : 'E-posta gönderilemedi'}</span>
        </div>

        {onMarkRead || onDelete || onRestore ? (
          <div className="flex flex-wrap gap-2">
            {onRestore ? (
              <Button variant="outline" disabled={restoring} onClick={onRestore}>
                {restoring ? 'Geri yükleniyor…' : 'Geri yükle'}
              </Button>
            ) : null}
            {onMarkRead && unread ? (
              <Button variant="outline" disabled={markingRead} onClick={onMarkRead}>
                {markingRead ? 'İşaretleniyor…' : 'Okundu işaretle'}
              </Button>
            ) : null}
            {onDelete ? (
              <Button variant="ghost" disabled={deleting} onClick={onDelete}>
                {deleting ? 'Siliniyor…' : 'Sil'}
              </Button>
            ) : null}
          </div>
        ) : null}
      </div>
    </Card>
  )
}
