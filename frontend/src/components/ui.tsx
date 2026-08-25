import type {
  HTMLAttributes,
  InputHTMLAttributes,
  ReactNode,
  SelectHTMLAttributes,
} from 'react'

/**
 * Kart kabı. Kalan öznitelikler `div`'e geçiriliyor: `data-testid` gibi
 * alanlar sessizce düşerse render doğrulaması kartı hiç bulamıyor — bu
 * hatayı Faz 5'te headless render yakaladı.
 */
export function Card({ children, className = '', ...rest }: HTMLAttributes<HTMLDivElement>) {
  return (
    <div
      {...rest}
      className={`rounded-xl border border-stone-200 bg-white shadow-sm dark:border-stone-800 dark:bg-stone-900 ${className}`}
    >
      {children}
    </div>
  )
}

export function Badge({
  children,
  tone = 'bg-brand-50 text-brand-800 dark:bg-brand-950 dark:text-brand-200',
}: {
  children: ReactNode
  tone?: string
}) {
  return (
    <span className={`inline-flex items-center rounded-full px-2.5 py-0.5 text-xs font-medium ${tone}`}>
      {children}
    </span>
  )
}

type ButtonProps = {
  children: ReactNode
  onClick?: () => void
  type?: 'button' | 'submit'
  variant?: 'primary' | 'ghost' | 'outline'
  disabled?: boolean
  className?: string
}

export function Button({
  children,
  onClick,
  type = 'button',
  variant = 'primary',
  disabled = false,
  className = '',
}: ButtonProps) {
  // T3KYS'teki `.btn-orange` davranışı: turuncu zemin + beyaz yazı, üzerine
  // gelince kızarıp koyulaşıyor. İkincil düğmeler de nötr griye değil aynı
  // turuncuya doğru açılıyor — hover'ın rengi tek yerden geliyor.
  const styles = {
    primary:
      'bg-brand-500 text-white hover:bg-brand-600 disabled:bg-stone-200 disabled:text-stone-400 dark:disabled:bg-stone-800',
    outline:
      'border border-stone-300 text-stone-700 hover:border-brand-500 hover:bg-brand-500 hover:text-white dark:border-stone-700 dark:text-stone-200',
    ghost:
      'text-stone-600 hover:bg-brand-50 hover:text-brand-700 dark:text-stone-300 dark:hover:bg-stone-800',
  }[variant]

  return (
    <button
      type={type}
      onClick={onClick}
      disabled={disabled}
      // Klavye odağı düğmelerde hiç görünmüyordu: tarayıcının varsayılan
      // halkası `transition-colors` ile birlikte fark edilmiyor, renk değişimi
      // de yalnızca hover'da vardı. `focus-visible` fare tıklamasında çıkmaz.
      className={`inline-flex items-center justify-center gap-2 rounded-lg px-4 py-2 text-sm font-medium transition-colors focus-visible:outline-2 focus-visible:outline-offset-2 focus-visible:outline-brand-600 disabled:cursor-not-allowed ${styles} ${className}`}
    >
      {children}
    </button>
  )
}

export function Input({ label, ...props }: { label?: string } & InputHTMLAttributes<HTMLInputElement>) {
  return (
    <label className="flex flex-col gap-1.5">
      {label ? (
        <span className="text-sm font-medium text-stone-700 dark:text-stone-300">{label}</span>
      ) : null}
      <input
        {...props}
        className="rounded-lg border border-stone-300 bg-white px-3 py-2 text-sm text-stone-900 outline-none placeholder:text-stone-400 focus:border-brand-500 focus:ring-2 focus:ring-brand-500/60 dark:border-stone-700 dark:bg-stone-950 dark:text-stone-100"
      />
    </label>
  )
}

export function Select({
  label,
  children,
  ...props
}: { label?: string; children: ReactNode } & SelectHTMLAttributes<HTMLSelectElement>) {
  return (
    <label className="flex flex-col gap-1.5">
      {label ? (
        <span className="text-sm font-medium text-stone-700 dark:text-stone-300">{label}</span>
      ) : null}
      <select
        {...props}
        className="rounded-lg border border-stone-300 bg-white px-3 py-2 text-sm text-stone-900 outline-none focus:border-brand-500 focus:ring-2 focus:ring-brand-500/60 dark:border-stone-700 dark:bg-stone-950 dark:text-stone-100"
      >
        {children}
      </select>
    </label>
  )
}

export function Spinner({ label }: { label?: string }) {
  return (
    <div className="flex items-center gap-3 text-sm text-stone-500">
      <span className="size-4 animate-spin rounded-full border-2 border-stone-300 border-t-brand-500" />
      {label ?? 'Yükleniyor…'}
    </div>
  )
}

export function EmptyState({ title, hint }: { title: string; hint?: string }) {
  return (
    <div className="flex flex-col items-center gap-2 rounded-xl border border-dashed border-stone-300 px-6 py-14 text-center dark:border-stone-700">
      <p className="font-medium text-stone-700 dark:text-stone-200">{title}</p>
      {hint ? <p className="max-w-md text-sm text-stone-500">{hint}</p> : null}
    </div>
  )
}

export function ErrorState({ message }: { message: string }) {
  return (
    <div className="rounded-xl border border-red-200 bg-red-50 px-4 py-3 text-sm text-red-800 dark:border-red-900 dark:bg-red-950 dark:text-red-200">
      {message}
    </div>
  )
}

/**
 * Hassas alanların tek görüntüleme noktası (KVKK).
 *
 * Üç durumu ayırır ve bu ayrım ürünün özü: `authorized=false` ise kullanıcı
 * "yetkiniz yok" görür, `authorized=true` ama değer boşsa "kayıt yok" görür.
 * Aynı yerde boş kutu göstermek, verinin olmadığı izlenimini verir ve
 * kullanıcıyı yanıltır.
 */
export function Sensitive({
  value,
  authorized,
  className = '',
}: {
  value: string | null | undefined
  authorized: boolean
  className?: string
}) {
  if (!authorized) {
    return (
      <span
        title="Bu alanı görme yetkiniz yok"
        className={`inline-flex items-center gap-1 text-sm text-stone-400 ${className}`}
      >
        <span aria-hidden>🔒</span>
        <span className="italic">yetkiniz yok</span>
      </span>
    )
  }

  if (value === null || value === undefined || value === '') {
    return <span className={`text-sm text-stone-400 ${className}`}>—</span>
  }

  return <span className={className}>{value}</span>
}

export function DataRow({ label, children }: { label: string; children: ReactNode }) {
  return (
    <div className="flex flex-col gap-0.5 py-2">
      <dt className="text-xs font-medium tracking-wide text-stone-500 uppercase">{label}</dt>
      <dd className="text-sm text-stone-900 dark:text-stone-100">{children}</dd>
    </div>
  )
}
