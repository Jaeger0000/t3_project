import { useEffect, useMemo, useRef, useState } from 'react'
import { useSearchParams } from 'react-router-dom'
import { Badge, Button, Card, EmptyState, ErrorState, Spinner } from '@/components/ui'
import { formatDate } from '@/lib/format'
import { useDocumentTitle } from '@/lib/useDocumentTitle'
import { defaultFilters, useStartups } from '@/features/startups/queries'
import StartupTile from '@/features/startups/StartupTile'
import type { AiChatMessageRow, StartupListItem } from '@/api/types'
import { assistantExamples, assistantModeLabel, assistantModeTone } from './labels'
import MarkdownText from './MarkdownText'
import { useChatConversation, useChatConversations, useSendChatMessage } from './queries'

/**
 * Çok turlu AI sohbeti.
 *
 * Geçmiş **sunucuda** duruyor; bu ekranda istemci tarafı ikinci bir kalıcı
 * kopya yok. Sebebi ürünün özü: bağlamı istemci gönderebilseydi hiç sorulmamış
 * bir turu "sorulmuş" gibi sunabilirdi. Ekran yalnızca hangi sohbetin seçili
 * olduğunu tutar, onu da URL'de tutar — yenilemede ve paylaşılan bağlantıda
 * aynı sohbet açılır.
 *
 * Cevabın dayandığı girişimler kimlik olarak geliyor (`startupIds`), nesne
 * olarak değil. Kartlar girişim listesi ucundan çiziliyor: maskeleme
 * (tutar yerine kilit) böylece tek yerde, listeyle aynı kuralda kalıyor.
 */
export default function AssistantChatPage() {
  useDocumentTitle('AI asistan')

  const [searchParams, setSearchParams] = useSearchParams()
  const conversationId = searchParams.get('sohbet')

  const [question, setQuestion] = useState('')
  // Gönderilen soru, sunucudan tazelenene kadar ekranda dursun.
  const [pendingQuestion, setPendingQuestion] = useState<string | null>(null)

  const inputRef = useRef<HTMLInputElement>(null)
  const bottomRef = useRef<HTMLDivElement>(null)
  // Yanıt gelince odağı kutuya geri ver: klavyeyle çalışan kullanıcı bir
  // sonraki soruyu sekme tuşuyla aramak zorunda kalmasın.
  const refocusAfterSend = useRef(false)

  const conversations = useChatConversations()
  const conversation = useChatConversation(conversationId)
  const send = useSendChatMessage()

  /*
   * Girişim kartları için tek bir liste çağrısı yetiyor: sohbet cevabı yalnızca
   * kimlik döndürüyor ve kimlik başına ayrı istek atmak hem yavaş hem gereksiz.
   * Kapsam ve maskeleme sunucuda uygulandığı için burada süzmek yetkiyi
   * gevşetmiyor — görme yetkisi olmayan girişim listeye zaten girmiyor.
   */
  const startups = useStartups({ ...defaultFilters, pageSize: 100 })
  const startupsById = useMemo(() => {
    const map = new Map<string, StartupListItem>()
    for (const startup of startups.data?.items ?? []) map.set(startup.id, startup)
    return map
  }, [startups.data])

  const messages = conversation.data?.messages ?? []

  const selectConversation = (id: string | null) => {
    setSearchParams(
      (previous) => {
        const next = new URLSearchParams(previous)
        if (id) next.set('sohbet', id)
        else next.delete('sohbet')
        return next
      },
      { replace: true },
    )
    setPendingQuestion(null)
    send.reset()
  }

  const submit = (value: string) => {
    const trimmed = value.trim()
    // Çift gönderim koruması: istek uçarken ikinci tur açılmaz.
    if (!trimmed || send.isPending) return

    setPendingQuestion(trimmed)
    setQuestion('')
    refocusAfterSend.current = true

    send.mutate(
      { conversationId, question: trimmed },
      {
        onSuccess: (reply) => {
          // Yeni sohbetin kimliği ancak ilk yanıtla öğreniliyor.
          if (reply.conversationId !== conversationId) {
            setSearchParams(
              (previous) => {
                const next = new URLSearchParams(previous)
                next.set('sohbet', reply.conversationId)
                return next
              },
              { replace: true },
            )
          }
          setPendingQuestion(null)
        },
        onError: () => {
          // Soru kutuya geri konuyor: kullanıcı yazdığını kaybetmesin.
          setQuestion(trimmed)
          setPendingQuestion(null)
        },
      },
    )
  }

  // Yeni satır geldiğinde kayıt kabı sona kaydırılır; yoksa cevap ekranın
  // altında görünmez kalıyor ve kullanıcı "yanıt gelmedi" sanıyor.
  useEffect(() => {
    bottomRef.current?.scrollIntoView({ block: 'end' })
  }, [messages.length, pendingQuestion, send.isPending])

  /*
   * Odak, isteğin bittiği render'da veriliyor — `onSuccess` içinde değil.
   * Kutu istek uçarken `disabled` ve React onu yeniden etkinleştirmeden
   * çağrılan `focus()` sessizce hiçbir şey yapmıyordu.
   */
  useEffect(() => {
    if (send.isPending || !refocusAfterSend.current) return
    refocusAfterSend.current = false
    inputRef.current?.focus()
  }, [send.isPending])

  const remaining = 500 - question.length

  return (
    <div className="flex flex-col gap-6">
      <header>
        <h1 className="text-2xl font-bold text-stone-900 dark:text-stone-50">AI asistan</h1>
        <p className="mt-1 text-sm text-stone-500">
          Ekosisteme doğal dille soru sorun. Yanıtlar yalnızca sizin görme
          yetkiniz olan kayıtlardan üretilir ve her cevabın altında hangi araca
          dayandığı yazar.
        </p>
      </header>

      <div className="grid gap-6 lg:grid-cols-[17rem_1fr]">
        {/* --- Sol sütun: kayıtlı sohbetler --- */}
        {/* `min-w-0`: ızgara hücresinin kendiliğinden en küçük genişliği
            içeriğin min-content'i kadardır ve uzun sohbet başlığı 375 px'te
            sayfayı yatay kaydırıyordu. */}
        <Card className="flex h-fit min-w-0 flex-col gap-3 p-4" data-testid="chat-conversations">
          <div className="flex items-center justify-between gap-2">
            <h2 className="text-sm font-semibold text-stone-900 dark:text-stone-100">
              Sohbetlerim
              {conversations.data ? (
                <span className="ml-1 font-normal text-stone-500">
                  ({conversations.data.totalCount})
                </span>
              ) : null}
            </h2>
            <Button variant="outline" onClick={() => selectConversation(null)}>
              Yeni sohbet
            </Button>
          </div>

          {conversations.isPending ? <Spinner label="Sohbetler yükleniyor…" /> : null}
          {conversations.error ? (
            <ErrorState message={conversations.error.message} error={conversations.error} />
          ) : null}

          {conversations.data && conversations.data.items.length === 0 ? (
            <p className="text-sm text-stone-500">
              Henüz kayıtlı sohbetiniz yok. İlk sorunuzu sorduğunuzda burada listelenir.
            </p>
          ) : null}

          {conversations.data && conversations.data.items.length > 0 ? (
            <ul className="flex flex-col gap-1">
              {conversations.data.items.map((item) => {
                const active = item.id === conversationId
                return (
                  <li key={item.id}>
                    <button
                      type="button"
                      aria-current={active ? 'true' : undefined}
                      onClick={() => selectConversation(item.id)}
                      className={`w-full rounded-lg px-3 py-2 text-left transition-colors focus-visible:outline-2 focus-visible:outline-offset-2 focus-visible:outline-brand-600 ${
                        active
                          ? 'bg-brand-50 text-brand-800 dark:bg-brand-950 dark:text-brand-200'
                          : 'text-stone-700 hover:bg-stone-100 dark:text-stone-200 dark:hover:bg-stone-800'
                      }`}
                    >
                      {/* Uzun başlık sütunu genişletmesin. */}
                      <span className="block truncate text-sm font-medium">
                        {active ? '▸ ' : ''}
                        {item.title}
                      </span>
                      <span className="mt-0.5 block text-xs text-stone-500">
                        {formatDate(item.lastMessageAt)} · {item.messageCount} mesaj
                      </span>
                    </button>
                  </li>
                )
              })}
            </ul>
          ) : null}
        </Card>

        {/* --- Sağ sütun: seçili sohbet --- */}
        <Card className="flex min-w-0 flex-col gap-4 p-4 sm:p-5" data-testid="chat-panel">
          {conversation.error ? (
            <ErrorState
              message={
                conversation.error.message === 'İstek başarısız oldu.'
                  ? 'Bu sohbet bulunamadı ya da size ait değil.'
                  : conversation.error.message
              }
              error={conversation.error}
            />
          ) : null}

          {/* Konuşma kabı ekranın çoğunu kaplasın: 60vh'de üç turluk sohbet
              bile kutunun içinde kaybolup sürekli kaydırma istiyordu.
              `min()` iki ucu birden tutuyor — kısa ekranda sayfayı taşırmıyor,
              geniş ekranda 34 rem'de duruyor (daha uzun satır okunmaz olurdu). */}
          <div
            role="log"
            aria-live="polite"
            aria-busy={send.isPending}
            aria-label="Sohbet geçmişi"
            className="flex h-[min(34rem,calc(100vh-16rem))] min-w-0 flex-col gap-4 overflow-y-auto"
            data-testid="chat-log"
          >
            {conversation.isPending && conversationId ? (
              <Spinner label="Sohbet yükleniyor…" />
            ) : null}

            {!conversationId && !pendingQuestion && !send.isPending ? (
              <EmptyState
                title="Ekosisteme bir soru sorun"
                hint="Örneğin bir sektördeki girişimleri, bir programdan geçenleri ya da yatırım almış kayıtları sorabilirsiniz."
              />
            ) : null}

            {messages.map((message) => (
              <ChatBubble key={message.id} message={message} startupsById={startupsById} />
            ))}

            {/* İyimser satır: soru sunucudan dönmeden de ekranda dursun. */}
            {pendingQuestion ? (
              <div className="flex justify-end">
                <div className="max-w-[85%] rounded-2xl rounded-br-sm bg-brand-500 px-4 py-2.5 text-base whitespace-pre-line text-white opacity-80">
                  {pendingQuestion}
                </div>
              </div>
            ) : null}

            {send.isPending ? (
              <div className="flex justify-start">
                <div className="rounded-2xl rounded-bl-sm bg-stone-100 px-4 py-2.5 dark:bg-stone-950">
                  <Spinner label="Asistan yazıyor…" />
                </div>
              </div>
            ) : null}

            <div ref={bottomRef} />
          </div>

          {send.error ? <ErrorState message={send.error.message} error={send.error} /> : null}

          <form
            className="flex flex-col gap-2 border-t border-stone-100 pt-4 dark:border-stone-800"
            onSubmit={(event) => {
              event.preventDefault()
              submit(question)
            }}
          >
            <div className="flex flex-wrap items-end gap-3">
              <label className="min-w-56 flex-1">
                <span className="sr-only">Sorunuz</span>
                <input
                  ref={inputRef}
                  value={question}
                  maxLength={500}
                  disabled={send.isPending}
                  onChange={(event) => setQuestion(event.target.value)}
                  placeholder="Örn. Yazılım sektöründeki girişimleri listele"
                  aria-label="Sorunuz"
                  className="w-full rounded-lg border border-stone-300 bg-white px-3 py-2 text-base text-stone-900 outline-none placeholder:text-stone-400 focus:border-brand-500 focus:ring-2 focus:ring-brand-500/60 disabled:bg-stone-100 dark:border-stone-700 dark:bg-stone-950 dark:text-stone-100 dark:disabled:bg-stone-900"
                />
              </label>
              <Button type="submit" disabled={send.isPending || question.trim().length === 0}>
                {send.isPending ? 'Gönderiliyor…' : 'Gönder'}
              </Button>
            </div>

            <p className="text-xs text-stone-500" aria-live="polite">
              {remaining} karakter kaldı (en çok 500).
            </p>
          </form>

          {/* Boş sohbette örnek sorular; tıklayınca kutuya yazılır, kullanıcı
              göndermeden önce düzenleyebilsin. */}
          {messages.length === 0 && !send.isPending ? (
            <div className="flex flex-wrap gap-2">
              {assistantExamples.map((example) => (
                <button
                  key={example}
                  type="button"
                  onClick={() => {
                    setQuestion(example)
                    inputRef.current?.focus()
                  }}
                  className="rounded-full border border-stone-200 px-3 py-1 text-xs text-stone-600 transition-colors hover:border-brand-500 hover:text-brand-700 focus-visible:outline-2 focus-visible:outline-offset-2 focus-visible:outline-brand-600 dark:border-stone-700 dark:text-stone-300"
                >
                  {example}
                </button>
              ))}
            </div>
          ) : null}
        </Card>
      </div>
    </div>
  )
}

/**
 * Tek mesaj balonu. Kullanıcı sağda, asistan solda; asistan balonunun altında
 * kaynaklar ve — varsa — cevabın dayandığı girişim kartları durur.
 */
function ChatBubble({
  message,
  startupsById,
}: {
  message: AiChatMessageRow
  startupsById: Map<string, StartupListItem>
}) {
  if (message.role === 'User') {
    return (
      <div className="flex justify-end">
        <div
          className="max-w-[85%] rounded-2xl rounded-br-sm bg-brand-500 px-4 py-2.5 text-base break-words whitespace-pre-line text-white"
          data-testid="chat-user-message"
        >
          {message.text}
        </div>
      </div>
    )
  }

  // Kimliği listede bulunmayan girişim atlanıyor: kapsam dışı bir kayıt
  // (ya da silinmiş kayıt) için kart çizmek yetki sınırını bulanıklaştırırdı.
  const cards = message.startupIds
    .map((id) => startupsById.get(id))
    .filter((startup): startup is StartupListItem => Boolean(startup))

  return (
    <div className="flex flex-col gap-3">
      <div className="flex justify-start">
        <div
          className="max-w-[85%] min-w-0 rounded-2xl rounded-bl-sm bg-stone-100 px-4 py-2.5 text-base break-words text-stone-900 dark:bg-stone-950 dark:text-stone-100"
          data-testid="chat-assistant-message"
        >
          {/* Model tablo ve kalın metin döndürüyor; ham basıldığında ekranda
              `|---|` ayraç satırları kalıyordu. */}
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
      </div>

      {/* Kartlar cevabın hemen altında duruyor, kaynak listesinin altında
          değil: kullanıcının asıl istediği eylem "o girişime git" ve metindeki
          ad tıklanabilir değil. Kimlikler modelden değil araç sonucundan
          geldiği için buradaki bağlantı uydurulmuş bir kayda gitmez. */}
      {cards.length > 0 ? (
        <div className="min-w-0">
          <p className="text-xs font-medium tracking-wide text-stone-500 uppercase">
            Cevaptaki girişimler
          </p>
          <div
            className="mt-1.5 grid min-w-0 gap-3 sm:grid-cols-2"
            data-testid="chat-startup-cards"
          >
            {cards.map((startup) => (
              <StartupTile key={startup.id} startup={startup} />
            ))}
          </div>
        </div>
      ) : null}

      {/* Kaynak listesi süs değil: AI burada karar verici değil karar *destek*
          katmanı, dolayısıyla her cevabın hangi araç çağrısından geldiği
          ekranda durmalı. Kalıcı geçmişte araç **adı** saklanıyor, panelin
          gösterdiği tek seferlik özet cümlesi değil — geçmişi açan kullanıcı
          yine dayanağı görüyor, üstelik kartlar cevabın hangi kayıtlara
          dayandığını adıyla söylüyor. */}
      {message.tools.length > 0 ? (
        <div className="min-w-0">
          <p className="text-xs font-medium tracking-wide text-stone-500 uppercase">Kaynaklar</p>
          <ul className="mt-1.5 flex flex-wrap gap-1.5">
            {message.tools.map((tool, index) => (
              <li key={`${tool}-${index}`}>
                <code className="rounded bg-stone-100 px-1.5 py-0.5 text-xs break-all text-stone-700 dark:bg-stone-800 dark:text-stone-300">
                  {tool}
                </code>
              </li>
            ))}
          </ul>
        </div>
      ) : null}

    </div>
  )
}
