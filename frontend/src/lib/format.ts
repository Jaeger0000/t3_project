const money = new Intl.NumberFormat('tr-TR', { maximumFractionDigits: 0 })
const compact = new Intl.NumberFormat('tr-TR', { notation: 'compact', maximumFractionDigits: 1 })
const longDate = new Intl.DateTimeFormat('tr-TR', { day: 'numeric', month: 'long', year: 'numeric' })
const monthYear = new Intl.DateTimeFormat('tr-TR', { month: 'long', year: 'numeric' })
const dateTime = new Intl.DateTimeFormat('tr-TR', {
  day: 'numeric',
  month: 'long',
  year: 'numeric',
  hour: '2-digit',
  minute: '2-digit',
})

/**
 * Tutar biçimlendirme. `null` iki farklı şey demek olabilir: kayıt yok ya da
 * görme yetkisi yok. Ayrımı çağıran taraf bilir (kartın `visibility` bloğu),
 * bu yüzden burada ikisi de aynı yer tutucuyla dönmez — `null` alan çağırana
 * geri verilir.
 */
export function formatMoney(amount: number | null | undefined, currency = 'TRY'): string | null {
  if (amount === null || amount === undefined) return null
  return `${money.format(amount)} ${currency === 'TRY' ? '₺' : currency}`
}

export function formatCompactMoney(amount: number | null | undefined, currency = 'TRY'): string | null {
  if (amount === null || amount === undefined) return null
  return `${compact.format(amount)} ${currency === 'TRY' ? '₺' : currency}`
}

export function formatDate(value: string | null | undefined): string | null {
  if (!value) return null
  return longDate.format(new Date(value))
}

/** Saatli tam tarih — bildirim/denetim izi gibi "ne zaman" tam olarak önemli olan yerlerde. */
export function formatDateTime(value: string | null | undefined): string | null {
  if (!value) return null
  return dateTime.format(new Date(value))
}

export function formatMonthYear(value: string | null | undefined): string | null {
  if (!value) return null
  return monthYear.format(new Date(value))
}

export function formatYear(value: string | null | undefined): string | null {
  if (!value) return null
  return String(new Date(value).getFullYear())
}
