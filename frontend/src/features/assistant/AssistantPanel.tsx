import { useState } from 'react'
import { Badge, Button, Card, ErrorState, Input, Spinner } from '@/components/ui'
import { useAskAssistant } from './queries'

const examples = [
  'Savunma sektöründe kaç girişim var?',
  "TEKNOFEST'ten geçmiş girişimler hangileri?",
  'En çok yatırım alan enerji girişimlerini listele',
]

/**
 * Doğal dil ekosistem sorgusu paneli.
 *
 * Yanıtın altındaki "kaynaklar" bloğu süs değil ürünün özü: AI burada karar
 * verici değil karar *destek* katmanı, dolayısıyla her cümlenin hangi araç
 * çağrısından geldiği ekranda durmalı. Model yapılandırılmamışsa yanıt yerel
 * planlayıcıdan gelir ve rozet bunu açıkça söyler — kullanıcı hangi modun
 * çalıştığını bilmeli.
 */
export default function AssistantPanel() {
  const [question, setQuestion] = useState('')
  const ask = useAskAssistant()

  const submit = (value: string) => {
    const trimmed = value.trim()
    if (!trimmed) return
    setQuestion(trimmed)
    ask.mutate(trimmed)
  }

  return (
    <Card className="p-5" data-testid="assistant-panel">
      <header className="flex flex-wrap items-center justify-between gap-3">
        <div>
          <h2 className="font-semibold text-slate-900 dark:text-slate-100">
            Ekosisteme soru sor
          </h2>
          <p className="mt-0.5 text-xs text-slate-500">
            Yanıtlar yalnızca sizin görme yetkiniz olan kayıtlardan üretilir.
          </p>
        </div>
        {ask.data ? (
          <Badge
            tone={
              ask.data.mode === 'Model'
                ? 'bg-emerald-100 text-emerald-800 dark:bg-emerald-950 dark:text-emerald-300'
                : 'bg-amber-100 text-amber-900 dark:bg-amber-950 dark:text-amber-200'
            }
          >
            {ask.data.mode === 'Model' ? `Model: ${ask.data.modelName}` : 'Yerel plan (model yok)'}
          </Badge>
        ) : null}
      </header>

      <form
        className="mt-4 flex flex-wrap items-end gap-3"
        onSubmit={(event) => {
          event.preventDefault()
          submit(question)
        }}
      >
        <div className="min-w-64 flex-1">
          <Input
            label="Soru"
            placeholder="Örn. Kuluçkadan geçip yatırım almış girişimler hangileri?"
            value={question}
            maxLength={500}
            onChange={(event) => setQuestion(event.target.value)}
          />
        </div>
        <Button type="submit" disabled={ask.isPending || question.trim().length === 0}>
          {ask.isPending ? 'Aranıyor…' : 'Sor'}
        </Button>
      </form>

      <div className="mt-3 flex flex-wrap gap-2">
        {examples.map((example) => (
          <button
            key={example}
            type="button"
            onClick={() => submit(example)}
            className="rounded-full border border-slate-200 px-3 py-1 text-xs text-slate-600 transition-colors hover:border-brand-500 hover:text-brand-600 dark:border-slate-700 dark:text-slate-300"
          >
            {example}
          </button>
        ))}
      </div>

      {ask.isPending ? <div className="mt-4"><Spinner label="Kayıtlar taranıyor…" /></div> : null}
      {ask.error ? <div className="mt-4"><ErrorState message={ask.error.message} /></div> : null}

      {ask.data ? (
        <div className="mt-5 flex flex-col gap-4">
          <div className="rounded-lg bg-slate-50 p-4 dark:bg-slate-950">
            <p className="text-xs text-slate-500">{ask.data.question}</p>
            <p
              className="mt-2 text-sm whitespace-pre-line text-slate-900 dark:text-slate-100"
              data-testid="assistant-answer"
            >
              {ask.data.answer}
            </p>
          </div>

          <div>
            <p className="text-xs font-medium tracking-wide text-slate-500 uppercase">
              Kaynaklar
            </p>
            {ask.data.sources.length === 0 ? (
              <p className="mt-1 text-sm text-slate-500">
                Bu yanıt için hiçbir kayıt sorgulanamadı.
              </p>
            ) : (
              <ul className="mt-1.5 flex flex-col gap-1.5">
                {ask.data.sources.map((source, index) => (
                  <li key={`${source.tool}-${index}`} className="flex flex-wrap items-baseline gap-2 text-sm">
                    <code className="rounded bg-slate-100 px-1.5 py-0.5 text-xs text-slate-700 dark:bg-slate-800 dark:text-slate-300">
                      {source.tool}
                    </code>
                    <span className="text-slate-600 dark:text-slate-300">{source.summary}</span>
                  </li>
                ))}
              </ul>
            )}
          </div>
        </div>
      ) : null}
    </Card>
  )
}
