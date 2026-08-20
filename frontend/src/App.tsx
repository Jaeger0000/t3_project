import { useQuery } from '@tanstack/react-query'
import { api } from '@/lib/apiClient'

type HealthResponse = {
  status: string
  service: string
  time: string
}

type DbHealthResponse = {
  database: string
  pendingMigrations: string[]
}

/**
 * Faz 0 doğrulama ekranı: frontend → Vite proxy → .NET API → PostgreSQL
 * zincirinin uçtan uca çalıştığını gösterir. Faz 1'de yerini login ve
 * yönlendirmeli uygulama kabuğu alacak.
 */
export default function App() {
  const health = useQuery({
    queryKey: ['health'],
    queryFn: () => api.get<HealthResponse>('/health'),
  })

  const db = useQuery({
    queryKey: ['health', 'db'],
    queryFn: () => api.get<DbHealthResponse>('/health/db'),
  })

  return (
    <main className="mx-auto flex min-h-screen max-w-3xl flex-col justify-center gap-8 px-6 py-16">
      <header>
        <p className="text-sm font-semibold tracking-widest text-brand-500 uppercase">
          T3 Vakfı · Yapay Zekâ Creathonu
        </p>
        <h1 className="mt-2 text-4xl font-bold text-brand-900 dark:text-brand-100">
          Girişim Ekosistemi Yönetim Sistemi
        </h1>
        <p className="mt-3 text-slate-600 dark:text-slate-400">
          Programdan yatırıma, T3 girişimcilik ekosisteminin tek kurumsal hafızası
          ve karar destek platformu.
        </p>
        <div className="mt-4 h-1 w-16 rounded bg-accent-500" />
      </header>

      <section className="grid gap-4 sm:grid-cols-2">
        <StatusCard
          title="API"
          loading={health.isPending}
          error={health.error?.message}
          value={health.data ? health.data.status : undefined}
          detail={health.data?.service}
        />
        <StatusCard
          title="Veritabanı"
          loading={db.isPending}
          error={db.error?.message}
          value={db.data ? db.data.database : undefined}
          detail={
            db.data
              ? db.data.pendingMigrations.length === 0
                ? 'Bekleyen migration yok'
                : `${db.data.pendingMigrations.length} bekleyen migration`
              : undefined
          }
        />
      </section>

      <footer className="text-sm text-slate-500">
        Faz 0 — iskelet ayakta. Sıradaki adım: kimlik doğrulama ve rol bazlı yetki (Faz 1).
      </footer>
    </main>
  )
}

type StatusCardProps = {
  title: string
  loading: boolean
  error?: string
  value?: string
  detail?: string
}

function StatusCard({ title, loading, error, value, detail }: StatusCardProps) {
  const state = loading ? 'loading' : error ? 'error' : 'ok'

  const dotClass = {
    loading: 'bg-slate-400 animate-pulse',
    error: 'bg-red-500',
    ok: 'bg-emerald-500',
  }[state]

  return (
    <article className="rounded-xl border border-slate-200 bg-white p-5 shadow-sm dark:border-slate-800 dark:bg-slate-900">
      <div className="flex items-center gap-2">
        <span className={`size-2.5 rounded-full ${dotClass}`} />
        <h2 className="font-semibold">{title}</h2>
      </div>
      <p className="mt-3 text-2xl font-bold tabular-nums">
        {loading ? '…' : error ? 'hata' : value}
      </p>
      <p className="mt-1 text-sm text-slate-500">{error ?? detail ?? ''}</p>
    </article>
  )
}
