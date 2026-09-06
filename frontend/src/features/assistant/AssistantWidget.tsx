import { useEffect, useMemo, useRef, useState } from 'react'
import { Link, useLocation } from 'react-router-dom'
import { Badge, Spinner } from '@/components/ui'
import { api, ApiError } from '@/lib/apiClient'
import { useAuth } from '@/lib/auth'
import { sectorLabels } from '@/lib/labels'
import { defaultFilters, useStartups } from '@/features/startups/queries'
import type { AiChatMessageRow, StartupListItem } from '@/api/types'
import { assistantExamples, assistantModeLabel, assistantModeTone } from './labels'
import MarkdownText from './MarkdownText'
import { useChatConversation, useSendChatMessage } from './queries'

/**
 * Her ekranın sağ altında duran sohbet baloncuğu.
 *
 * Neden ayrı ekranın yanında bir de bu var: kullanıcı soruyu aklına geldiği
 * yerde soruyor — girişim listesine bakarken "bunlardan hangisi 2024'te
 * yatırım aldı" diye sormak için sayfayı terk etmesi gerekmemeli. Bu yüzden
 * "Genişlet" düğmesi de başka bir ekrana **götürmüyor**, yalnızca paneli
 * büyütüyor: uzun bir tabloyu okumak için arkadaki listeyi kaybetmek gerekmez.
 * Kayıtlı sohbet listesiyle birlikte duran tam ekran asistan menüde ayrıca var
 * ve aynı sunucu geçmişine bakıyor — ayrı bir kopya yok.
 *
 * Sohbet kimliği bileşen state'inde: AppShell ekranlar arasında yeniden
 * kurulmadığı için kullanıcı sayfa gezerken aynı konuşmayı sürdürüyor.
 * Sayfa yenilenirse baloncuk boş açılır — geçmiş kaybolmuyor, sunucuda
 * duruyor ve menüdeki "AI asistan" ekranından açılıyor.
 */
export default function AssistantWidget() {
  const { session } = useAuth()
  const location = useLocation()

  const [open, setOpen] = useState(false)
  // "Genişlet" paneli büyütür, başka bir ekrana **götürmez**: kullanıcı cevabı
  // okurken arkadaki listeyi kaybetmek istemiyor. Tam ekran asistan menüde
  // ayrıca duruyor.
  const [wide, setWide] = useState(false)
  const [conversationId, setConversationId] = useState<string | null>(null)
  const [question, setQuestion] = useState('')
  const [pendingQuestion, setPendingQuestion] = useState<string | null>(null)

  const launcherRef = useRef<HTMLButtonElement>(null)
  const inputRef = useRef<HTMLInputElement>(null)
  const bottomRef = useRef<HTMLDivElement>(null)

  const conversation = useChatConversation(open ? conversationId : null)
  const send = useSendChatMessage()
  // `?? []` her render'da yeni dizi üretiyor; aşağıdaki useMemo o yüzden hiç
  // önbelleğe alamıyordu (kimlik kümesi ve harita her render'da yeniden kurulur).
  const messages = useMemo(() => conversation.data?.messages ?? [], [conversation.data])

  /*
   * Kartlar için girişim adları gerekiyor ve cevap yalnızca kimlik döndürüyor.
   * Liste ancak bir cevapta girişim geçtiğinde çekiliyor: baloncuk her ekranda
   * duruyor, açılmadan istek üretmemeli.
   */
  const referencedIds = useMemo(
    () => new Set(messages.flatMap((message) => message.startupIds)),
    [messages],
  )
  const startups = useStartups({ ...defaultFilters, pageSize: 100 }, referencedIds.size > 0)
  const startupsById = useMemo(() => {
    const map = new Map<string, StartupListItem>()
    for (const startup of startups.data?.items ?? []) map.set(startup.id, startup)
    return map
  }, [startups.data])

  // Açılışta odak kutuya; kapanışta baloncuğa geri. Klavyeyle çalışan
  // kullanıcı panelin açıldığını ancak odak oraya gidince anlıyor.
  useEffect(() => {
    if (open) inputRef.current?.focus()
    else launcherRef.current?.focus()
  }, [open])

  // Esc kapatır: yüzen panelin sayfayı örttüğü tek durum dar ekran ve
  // oradan çıkışın klavyeyle de bir yolu olmalı.
  useEffect(() => {
    if (!open) return
    const onKey = (event: KeyboardEvent) => {
      if (event.key === 'Escape') setOpen(false)
    }
    window.addEventListener('keydown', onKey)
    return () => window.removeEventListener('keydown', onKey)
  }, [open])

  useEffect(() => {
    bottomRef.current?.scrollIntoView({ block: 'end' })
  }, [messages.length, pendingQuestion, send.isPending, open])

  const submit = (value: string) => {
    const trimmed = value.trim()
    if (!trimmed || send.isPending) return

    setPendingQuestion(trimmed)
    setQuestion('')

    send.mutate(
      { conversationId, question: trimmed },
      {
        onSuccess: (reply) => {
          setConversationId(reply.conversationId)
          setPendingQuestion(null)
          inputRef.current?.focus()
        },
        onError: () => {
          setQuestion(trimmed)
          setPendingQuestion(null)
          inputRef.current?.focus()
        },
      },
    )
  }

  // Oturumsuz ekranlarda (giriş, KVKK metni) baloncuk anlamsız; tam ekran
  // asistanın üstünde ise ikinci bir kopya olurdu.
  if (!session || location.pathname.startsWith('/asistan')) return null

  return (
    <>
      {open ? (
        <section
          // `dialog` ama `aria-modal` değil: panel açıkken sayfa kullanılabilir
          // kalıyor, kullanıcı cevaba bakarken arkadaki listeyi süzebiliyor.
          role="dialog"
          aria-label="AI asistan sohbeti"
          data-testid="assistant-widget"
          data-wide={wide ? 'true' : 'false'}
          // Dar ekranda iki ölçü de aynı kapağa çarpıyor (`100vw-2rem`), yani
          // "genişlet" telefonda bir şeyi taşırmıyor; kazanç masaüstünde.
          className={`fixed right-4 bottom-20 z-40 flex flex-col overflow-hidden rounded-2xl border border-stone-200 bg-white shadow-2xl transition-[width,height] duration-200 dark:border-stone-700 dark:bg-stone-900 ${
            wide
              ? 'h-[calc(100vh-6rem)] w-[min(46rem,calc(100vw-2rem))]'
              : 'h-[min(40rem,calc(100vh-7rem))] w-[min(30rem,calc(100vw-2rem))]'
          }`}
        >
          <header className="flex items-center gap-2 border-b border-stone-200 px-4 py-3 dark:border-stone-700">
            <h2 className="flex-1 text-sm font-semibold text-stone-900 dark:text-stone-100">
              AI asistan
            </h2>
            <button
              type="button"
              onClick={() => {
                setConversationId(null)
                setPendingQuestion(null)
                send.reset()
                inputRef.current?.focus()
              }}
              className="rounded px-2 py-1 text-xs text-stone-600 hover:bg-stone-100 focus-visible:outline-2 focus-visible:outline-offset-2 focus-visible:outline-brand-600 dark:text-stone-300 dark:hover:bg-stone-800"
            >
              Yeni sohbet
            </button>
            <button
              type="button"
              onClick={() => setWide((previous) => !previous)}
              aria-pressed={wide}
              data-testid="assistant-widget-expand"
              className="rounded px-2 py-1 text-xs text-brand-700 hover:bg-stone-100 focus-visible:outline-2 focus-visible:outline-offset-2 focus-visible:outline-brand-600 dark:text-brand-300 dark:hover:bg-stone-800"
            >
              {wide ? 'Küçült ⤡' : 'Genişlet ⤢'}
            </button>
            <button
              type="button"
              onClick={() => setOpen(false)}
              aria-label="Sohbeti kapat"
              className="rounded px-2 py-1 text-stone-500 hover:bg-stone-100 focus-visible:outline-2 focus-visible:outline-offset-2 focus-visible:outline-brand-600 dark:hover:bg-stone-800"
            >
              ✕
            </button>
          </header>

          <div
            role="log"
            aria-live="polite"
            aria-busy={send.isPending}
            className="flex flex-1 flex-col gap-3 overflow-y-auto px-4 py-3"
          >
            {messages.length === 0 && !pendingQuestion ? (
              <div className="flex flex-col gap-2">
                <p className="text-sm text-stone-500">
                  Ekosisteme doğal dille sorun. Yanıtlar yalnızca sizin görme
                  yetkiniz olan kayıtlardan üretilir.
                </p>
                {assistantExamples.map((example) => (
                  <button
                    key={example}
                    type="button"
                    onClick={() => submit(example)}
                    className="rounded-lg border border-stone-200 px-3 py-2 text-left text-sm text-stone-600 transition-colors hover:border-brand-500 hover:text-brand-700 focus-visible:outline-2 focus-visible:outline-offset-2 focus-visible:outline-brand-600 dark:border-stone-700 dark:text-stone-300"
                  >
                    {example}
                  </button>
                ))}
              </div>
            ) : null}

            {conversation.isPending && conversationId ? (
              <Spinner label="Sohbet yükleniyor…" />
            ) : null}

            {messages.map((message) => (
              <WidgetBubble
                key={message.id}
                message={message}
                startupsById={startupsById}
                wide={wide}
                onNavigate={() => setOpen(false)}
              />
            ))}

            {pendingQuestion ? (
              <div className="flex justify-end">
                <div className="max-w-[85%] rounded-2xl rounded-br-sm bg-brand-500 px-3 py-2 text-sm whitespace-pre-line text-white opacity-80">
                  {pendingQuestion}
                </div>
              </div>
            ) : null}

            {send.isPending ? <Spinner label="Kayıtlar taranıyor…" /> : null}
            {send.error ? (
              <p className="rounded-lg bg-red-50 px-3 py-2 text-sm text-red-700 dark:bg-red-950 dark:text-red-300">
                {send.error.message}
              </p>
            ) : null}

            <div ref={bottomRef} />
          </div>

          <form
            className="flex items-end gap-2 border-t border-stone-200 px-3 py-3 dark:border-stone-700"
            onSubmit={(event) => {
              event.preventDefault()
              submit(question)
            }}
          >
            <input
              ref={inputRef}
              value={question}
              maxLength={500}
              disabled={send.isPending}
              onChange={(event) => setQuestion(event.target.value)}
              aria-label="Sorunuz"
              placeholder="Bir soru yazın…"
              className="min-w-0 flex-1 rounded-lg border border-stone-300 px-3 py-2 text-base text-stone-900 focus-visible:outline-2 focus-visible:outline-offset-1 focus-visible:outline-brand-600 disabled:opacity-60 dark:border-stone-600 dark:bg-stone-950 dark:text-stone-100"
            />
            <button
              type="submit"
              disabled={send.isPending || question.trim().length === 0}
              className="rounded-lg bg-brand-600 px-3 py-2 text-sm font-medium text-white transition-colors hover:bg-brand-700 focus-visible:outline-2 focus-visible:outline-offset-2 focus-visible:outline-brand-600 disabled:opacity-50"
            >
              Gönder
            </button>
          </form>
        </section>
      ) : null}

      <button
        ref={launcherRef}
        type="button"
        onClick={() => setOpen((previous) => !previous)}
        aria-expanded={open}
        aria-label={open ? 'AI asistanı kapat' : 'AI asistanı aç'}
        data-testid="assistant-launcher"
        className="fixed right-4 bottom-4 z-40 flex h-14 w-14 items-center justify-center rounded-full bg-brand-600 text-2xl text-white shadow-lg transition-transform hover:scale-105 hover:bg-brand-700 focus-visible:outline-2 focus-visible:outline-offset-2 focus-visible:outline-brand-600"
      >
        <span aria-hidden>{open ? '✕' : '💬'}</span>
      </button>
    </>
  )
}

/**
 * Baloncuk içindeki tek tur. Girişimler burada tam kart değil tek satırlık
 * bağlantı: panel 384 px ve kart ızgarası okunmaz hâle gelirdi. Kimlik
 * listede bulunmuyorsa satır çizilmiyor — kapsam dışı bir kayda bağlantı
 * vermek yetki sınırını bulanıklaştırırdı.
 */
function WidgetBubble({
  message,
  startupsById,
  wide,
  onNavigate,
}: {
  message: AiChatMessageRow
  startupsById: Map<string, StartupListItem>
  wide: boolean
  onNavigate: () => void
}) {
  // Genişletilmiş panelde yazı bir punto daha büyüyor: 46 rem genişlikte
  // küçük metin satır uzunluğunu okunmaz hâle getiriyordu.
  const metin = wide ? 'text-base' : 'text-sm'
  if (message.role === 'User') {
    return (
      <div className="flex justify-end">
        <div className={`max-w-[85%] rounded-2xl rounded-br-sm bg-brand-500 px-3 py-2 break-words whitespace-pre-line text-white ${metin}`}>
          {message.text}
        </div>
      </div>
    )
  }

  const cards = message.startupIds
    .map((id) => startupsById.get(id))
    .filter((startup): startup is StartupListItem => Boolean(startup))

  return (
    <div className="flex flex-col gap-2">
      <div className={`max-w-[92%] min-w-0 rounded-2xl rounded-bl-sm bg-stone-100 px-3 py-2 break-words text-stone-900 dark:bg-stone-950 dark:text-stone-100 ${metin}`}>
        <MarkdownText text={message.text} />
        {message.mode ? (
          <div className="mt-2">
            <Badge tone={assistantModeTone[message.mode]}>
              {message.mode === 'Model' ? '● ' : '◐ '}
              {assistantModeLabel(message.mode, message.modelName)}
            </Badge>
          </div>
        ) : null}
      </div>

      {cards.length > 0 ? (
        <div className="min-w-0" data-testid="widget-startup-links">
          <p className="text-xs font-medium tracking-wide text-stone-500 uppercase">
            Cevaptaki girişimler
          </p>
          <ul className={`mt-1 grid gap-1 ${wide ? 'sm:grid-cols-2' : ''}`}>
            {cards.map((startup) => (
              <li key={startup.id}>
                <Link
                  to={`/girisimler/${startup.id}`}
                  onClick={onNavigate}
                  className="flex items-center gap-2 rounded-lg border border-stone-200 px-2.5 py-1.5 transition-colors hover:border-brand-500 hover:bg-brand-50 focus-visible:outline-2 focus-visible:outline-offset-2 focus-visible:outline-brand-600 dark:border-stone-700 dark:hover:bg-stone-800"
                >
                  <span className="min-w-0 flex-1 truncate text-sm font-medium text-stone-900 dark:text-stone-100">
                    {startup.name}
                  </span>
                  <span className="shrink-0 text-xs text-stone-500">
                    {sectorLabels[startup.sector]}
                  </span>
                  <span aria-hidden className="shrink-0 text-brand-700 dark:text-brand-300">
                    →
                  </span>
                </Link>
              </li>
            ))}
          </ul>
        </div>
      ) : null}

      {message.downloadToken ? <WidgetExportButton message={message} /> : null}
    </div>
  )
}

/** Genişlikte tam düğme yerine, dar baloncuğa sığan küçük bağlantı-benzeri buton. */
function WidgetExportButton({ message }: { message: AiChatMessageRow }) {
  const [busy, setBusy] = useState(false)
  const [error, setError] = useState<string | null>(null)

  async function handleDownload() {
    if (!message.downloadToken) return
    setBusy(true)
    setError(null)
    try {
      await api.download(
        `/api/ai/exports/${message.downloadToken}`,
        message.downloadFileName ?? 'disa-aktarma.xlsx',
      )
    } catch (err) {
      setError(
        err instanceof ApiError
          ? err.status === 404
            ? 'Bağlantının süresi doldu.'
            : err.message
          : 'Dosya indirilemedi.',
      )
    } finally {
      setBusy(false)
    }
  }

  return (
    <div className="flex flex-col items-start gap-1">
      <button
        type="button"
        onClick={handleDownload}
        disabled={busy}
        className="rounded-lg border border-brand-200 bg-brand-50 px-2.5 py-1.5 text-xs font-medium text-brand-800 transition-colors hover:border-brand-500 focus-visible:outline-2 focus-visible:outline-offset-2 focus-visible:outline-brand-600 disabled:opacity-60 dark:border-brand-800 dark:bg-brand-950 dark:text-brand-200"
      >
        {busy ? 'İndiriliyor…' : `⬇ ${message.downloadFileName ?? 'Dosyayı indir'}`}
      </button>
      {error ? <p className="text-xs text-red-600 dark:text-red-400">{error}</p> : null}
    </div>
  )
}
