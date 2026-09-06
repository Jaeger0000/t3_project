import { useState } from 'react'
import { ApiError } from '@/lib/apiClient'
import { Button, Card, ErrorState } from '@/components/ui'
import { useSendNotification } from '@/features/notifications/queries'

/**
 * Girişime serbest metinli bildirim gönderme paneli. Gönderilen bildirim
 * hem alıcının uygulama içi "Bildirimler" ekranında hem de e-postasında
 * görünür (bkz. backend SendNotificationHandler).
 */
export default function SendNotificationPanel({
  startupId,
  onClose,
}: {
  startupId: string
  onClose: () => void
}) {
  const [message, setMessage] = useState('')
  const send = useSendNotification(startupId)

  const handleSend = () => {
    const trimmed = message.trim()
    if (!trimmed) return

    send.mutate(trimmed, {
      onSuccess: () => {
        setMessage('')
        onClose()
      },
    })
  }

  return (
    <Card className="p-5">
      <h2 className="font-semibold text-stone-900 dark:text-stone-100">Bildirim Gönder</h2>
      <p className="mt-1 text-sm text-stone-500">
        Girişime bir mesaj gönderin — hem e-posta olarak gider hem de girişimin
        "Bildirimler" ekranında görünür.
      </p>

      <label className="mt-4 flex flex-col gap-1.5">
        <span className="text-sm font-medium text-stone-700 dark:text-stone-300">Mesaj</span>
        <textarea
          rows={3}
          maxLength={2000}
          disabled={send.isPending}
          placeholder='Örn. "Ciro belgenizi lütfen bu hafta içinde güncelleyin."'
          value={message}
          onChange={(e) => setMessage(e.target.value)}
          className="rounded-lg border border-stone-300 bg-white px-3 py-2 text-sm text-stone-900 outline-none focus:border-brand-500 focus:ring-2 focus:ring-brand-500/60 disabled:opacity-60 dark:border-stone-700 dark:bg-stone-950 dark:text-stone-100"
        />
      </label>

      {send.error ? (
        <div className="mt-4">
          <ErrorState
            message={send.error instanceof ApiError ? send.error.message : 'Bildirim gönderilemedi.'}
            error={send.error}
          />
        </div>
      ) : null}

      <div className="mt-5 flex flex-wrap gap-3">
        <Button disabled={send.isPending || message.trim().length === 0} onClick={handleSend}>
          {send.isPending ? 'Gönderiliyor…' : 'Gönder'}
        </Button>
        <Button variant="outline" disabled={send.isPending} onClick={onClose}>
          Vazgeç
        </Button>
      </div>
    </Card>
  )
}
