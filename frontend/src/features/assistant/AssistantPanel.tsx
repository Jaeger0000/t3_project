import { useMemo, useState } from 'react'
import { Link } from 'react-router-dom'
import { Badge, Button, Card, ErrorState, Input, Spinner } from '@/components/ui'
import { defaultFilters, useStartups } from '@/features/startups/queries'
import StartupTile from '@/features/startups/StartupTile'
import type { StartupListItem } from '@/api/types'
import { assistantExamples as examples } from './labels'
import MarkdownText from './MarkdownText'
import { useAskAssistant } from './queries'

/**
 * Doğal dil ekosistem sorgusu paneli.
 *
 * Yanıtın altındaki "kaynaklar" bloğu süs değil ürünün özü: AI burada karar
 * verici değil karar *destek* katmanı, dolayısıyla her cümlenin hangi araç
 * çağrısından geldiği ekranda durmalı. Model yapılandırılmamışsa yanıt yerel
 * planlayıcıdan gelir ve rozet bunu açıkça söyler — kullanıcı hangi modun
 * çalıştığını bilmeli.
 *
 * Model artık uygulamanın içinden de bağlanıyor (OpenRouter); ekosistem ayrıca
 * <b>MCP sunucusu</b> olarak açık duruyor, yani harici bir ajan kendi jetonuyla
 * aynı araçları kullanabiliyor. Anahtar tanımlı değilse yanıt yerel
 * planlayıcıdan gelir ve aşağıdaki bilgi notu bunu ekranda söyler — "AI
 * çalışıyor sanılması" Dalga 0'da tam olarak bu sessizlikten çıkmıştı.
 */
export default function AssistantPanel() {
  const [question, setQuestion] = useState('')
  const ask = useAskAssistant()

  /*
   * Cevap yalnızca kimlik döndürüyor; kart çizmek için girişim listesi bir kez
   * çekiliyor ve ancak bir cevap geldiğinde. Kapsam ve maskeleme sunucuda
   * uygulandığı için burada eşleştirmek yetkiyi gevşetmiyor: görme yetkisi
   * olmayan girişim listeye zaten girmiyor, kimliği eşleşmeyen kart çizilmiyor.
   */
  const answerStartupIds = useMemo(
    () => [...new Set((ask.data?.sources ?? []).flatMap((source) => source.startupIds))],
    [ask.data],
  )
  const startups = useStartups({ ...defaultFilters, pageSize: 100 }, answerStartupIds.length > 0)
  const cards = useMemo(() => {
    if (answerStartupIds.length === 0) return []
    const byId = new Map<string, StartupListItem>()
    for (const startup of startups.data?.items ?? []) byId.set(startup.id, startup)
    return answerStartupIds
      .map((id) => byId.get(id))
      .filter((startup): startup is StartupListItem => Boolean(startup))
  }, [answerStartupIds, startups.data])

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
          <h2 className="font-semibold text-stone-900 dark:text-stone-100">
            Ekosisteme soru sor
          </h2>
          <p className="mt-0.5 text-xs text-stone-500">
            Yanıtlar yalnızca sizin görme yetkiniz olan kayıtlardan üretilir.
          </p>
        </div>
        {ask.data ? (
          <Badge
            tone={
              ask.data.mode === 'Model'
                ? 'bg-emerald-50 text-emerald-700 dark:bg-emerald-950 dark:text-emerald-300'
                : 'bg-brand-100 text-brand-800 dark:bg-brand-950 dark:text-brand-200'
            }
          >
            {ask.data.mode === 'Model' ? `Model: ${ask.data.modelName}` : 'Yerel plan (model yok)'}
          </Badge>
        ) : null}
      </header>

      {/* Bu not yalnızca yerel planlayıcı yanıtladığında çıkıyor. Önceden
          soru sorulmadan da görünüyordu ve "anahtar tanımlı değil" diyordu;
          anahtar tanımlıyken bu cümle ekranda yanlış bilgi oluyordu — hangi
          modun yanıtladığı ancak yanıt geldiğinde bilinir. */}
      {ask.data && ask.data.mode !== 'Model' ? (
        <p className="mt-3 rounded-lg bg-stone-50 px-3 py-2 text-xs text-stone-600 dark:bg-stone-900 dark:text-stone-300">
          <strong>Bu yanıt model olmadan üretildi.</strong> Uygulamada bir dil
          modeli anahtarı tanımlı değil; sistem{' '}
          <code className="rounded bg-stone-100 px-1 dark:bg-stone-800">POST /mcp</code>{' '}
          ucuyla MCP sunucusu olarak da yayımlanıyor ve harici bir ajan (Claude
          Desktop, Claude Code vb.) kendi jetonuyla bağlanıp aşağıdaki araçların
          aynısını kullanıyor — kendi yetkisi kadar görerek. Panelden gelen bu
          yanıt ise yerel anahtar sözcük planlayıcısından geliyor.
        </p>
      ) : null}

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

      {/* Panodaki panel tek soruluk: bağlam süren, kayıtlı sohbet ayrı ekranda
          duruyor ve kullanıcı oraya buradan geçiyor. */}
      <p className="mt-3 text-sm">
        <Link
          to="/asistan"
          className="rounded text-brand-700 hover:underline focus-visible:outline-2 focus-visible:outline-offset-2 focus-visible:outline-brand-600 dark:text-brand-300"
        >
          Sohbete geç →
        </Link>{' '}
        <span className="text-xs text-stone-500">
          (art arda soru sorun, geçmiş kayıtlı kalsın)
        </span>
      </p>

      <div className="mt-3 flex flex-wrap gap-2">
        {examples.map((example) => (
          <button
            key={example}
            type="button"
            onClick={() => submit(example)}
            className="rounded-full border border-stone-200 px-3 py-1 text-xs text-stone-600 transition-colors hover:border-brand-500 hover:text-brand-700 dark:border-stone-700 dark:text-stone-300"
          >
            {example}
          </button>
        ))}
      </div>

      {ask.isPending ? <div className="mt-4"><Spinner label="Kayıtlar taranıyor…" /></div> : null}
      {ask.error ? <div className="mt-4"><ErrorState message={ask.error.message} error={ask.error} /></div> : null}

      {ask.data ? (
        <div className="mt-5 flex flex-col gap-4">
          <div className="rounded-lg bg-stone-50 p-4 dark:bg-stone-950">
            <p className="text-xs text-stone-500">{ask.data.question}</p>
            {/* Model tablo/kalın metin döndürüyor; ham basıldığında ekranda
                `|---|` ayraç satırları ve boş hücreler kalıyordu. */}
            <MarkdownText
              text={ask.data.answer}
              className="mt-2 text-sm text-stone-900 dark:text-stone-100"
              data-testid="assistant-answer"
            />
          </div>

          {/* Cevapta geçen girişimlere tıklanabilir kart: metindeki ad
              bağlantı değil, kullanıcının asıl istediği eylem ise "oraya git".
              Kimlikler modelden değil araç sonucundan geldiği için kart
              uydurulmuş bir kayda gitmez. */}
          {cards.length > 0 ? (
            <div>
              <p className="text-xs font-medium tracking-wide text-stone-500 uppercase">
                Cevaptaki girişimler
              </p>
              <div
                className="mt-1.5 grid gap-3 sm:grid-cols-2"
                data-testid="assistant-startup-cards"
              >
                {cards.map((startup) => (
                  <StartupTile key={startup.id} startup={startup} />
                ))}
              </div>
            </div>
          ) : null}

          <div>
            <p className="text-xs font-medium tracking-wide text-stone-500 uppercase">
              Kaynaklar
            </p>
            {ask.data.sources.length === 0 ? (
              <p className="mt-1 text-sm text-stone-500">
                Bu yanıt için hiçbir kayıt sorgulanamadı.
              </p>
            ) : (
              <ul className="mt-1.5 flex flex-col gap-1.5">
                {ask.data.sources.map((source, index) => (
                  <li key={`${source.tool}-${index}`} className="flex flex-wrap items-baseline gap-2 text-sm">
                    <code className="rounded bg-stone-100 px-1.5 py-0.5 text-xs text-stone-700 dark:bg-stone-800 dark:text-stone-300">
                      {source.tool}
                    </code>
                    <span className="text-stone-600 dark:text-stone-300">{source.summary}</span>
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
