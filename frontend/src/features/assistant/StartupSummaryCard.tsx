import { useState } from 'react'
import { Badge, Button, Card, ErrorState, Spinner } from '@/components/ui'
import { useStartupSummary } from './queries'

/**
 * Girişim kartındaki AI özeti.
 *
 * Kullanıcı tıklamadan çağrılmıyor: model yapılandırıldığında her kart açılışı
 * bir dış istek üretirdi. Özetin altında "dayanak" listesi durur — özet, kartın
 * kendi verisinden türetildiğini gösteremezse karar desteği değil, doğrulanamaz
 * bir iddia olur.
 */
export default function StartupSummaryCard({ startupId }: { startupId: string }) {
  const [requested, setRequested] = useState(false)
  const summary = useStartupSummary(startupId, requested)

  return (
    <Card className="p-5" data-testid="ai-summary">
      <header className="flex flex-wrap items-center justify-between gap-3">
        <div>
          <h2 className="font-semibold text-slate-900 dark:text-slate-100">Yönetici özeti</h2>
          <p className="mt-0.5 text-xs text-slate-500">
            Kart ve gelişim yolculuğundaki kayıtlardan üretilir; maskeli alanlar özete girmez.
          </p>
        </div>
        {summary.data ? (
          <Badge
            tone={
              summary.data.mode === 'Model'
                ? 'bg-emerald-100 text-emerald-800 dark:bg-emerald-950 dark:text-emerald-300'
                : 'bg-amber-100 text-amber-900 dark:bg-amber-950 dark:text-amber-200'
            }
          >
            {summary.data.mode === 'Model' ? `Model: ${summary.data.modelName}` : 'Yerel özet'}
          </Badge>
        ) : null}
      </header>

      {!requested ? (
        <Button className="mt-4" variant="outline" onClick={() => setRequested(true)}>
          ✨ Özet oluştur
        </Button>
      ) : null}

      {summary.isPending && requested ? <div className="mt-4"><Spinner label="Özet hazırlanıyor…" /></div> : null}
      {summary.error ? <div className="mt-4"><ErrorState message={summary.error.message} /></div> : null}

      {summary.data ? (
        <div className="mt-4 flex flex-col gap-4">
          <p
            className="text-sm leading-relaxed text-slate-700 dark:text-slate-200"
            data-testid="ai-summary-text"
          >
            {summary.data.summary}
          </p>

          <div>
            <p className="text-xs font-medium tracking-wide text-slate-500 uppercase">Dayanak</p>
            <ul className="mt-1.5 flex flex-col gap-1 text-sm text-slate-600 dark:text-slate-300">
              {summary.data.highlights.map((line) => (
                <li key={line}>• {line}</li>
              ))}
            </ul>
            {!summary.data.exactAmountsVisible ? (
              <p className="mt-2 text-xs text-amber-700 dark:text-amber-300">
                🔒 Tutarları görme yetkiniz yok; özet tutar içermez.
              </p>
            ) : null}
          </div>
        </div>
      ) : null}
    </Card>
  )
}
