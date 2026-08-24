/**
 * Tek HTTP giriş noktası. Jeton saklama ve hata normalleştirme burada durur;
 * özellik klasörleri yalnızca `api.get/post` çağırır.
 */

const TOKEN_KEY = 't3.accessToken'

export class ApiError extends Error {
  readonly status: number
  readonly details?: string[]

  constructor(status: number, message: string, details?: string[]) {
    super(message)
    this.name = 'ApiError'
    this.status = status
    this.details = details
  }
}

export const tokenStore = {
  get: () => localStorage.getItem(TOKEN_KEY),
  set: (token: string) => localStorage.setItem(TOKEN_KEY, token),
  clear: () => localStorage.removeItem(TOKEN_KEY),
}

async function request<T>(path: string, init: RequestInit = {}): Promise<T> {
  const token = tokenStore.get()

  const response = await fetch(path, {
    ...init,
    headers: {
      'Content-Type': 'application/json',
      ...(token ? { Authorization: `Bearer ${token}` } : {}),
      ...init.headers,
    },
  })

  if (response.status === 401) {
    tokenStore.clear()
    throw new ApiError(401, 'Oturum süresi doldu, tekrar giriş yapın.')
  }

  if (!response.ok) {
    const body = await response.json().catch(() => null)
    throw new ApiError(
      response.status,
      body?.title ?? 'İstek başarısız oldu.',
      body?.errors ?? undefined,
    )
  }

  if (response.status === 204) {
    return undefined as T
  }

  return (await response.json()) as T
}

/**
 * Dosya yükleme. `Content-Type` bilinçli olarak ayarlanmıyor: multipart
 * sınırlayıcısını (boundary) tarayıcı üretir, elle yazılan başlık onu ezip
 * gövdeyi ayrıştırılamaz hâle getirir.
 */
async function upload<T>(path: string, file: File, query = ''): Promise<T> {
  const token = tokenStore.get()
  const form = new FormData()
  form.append('file', file)

  const response = await fetch(path + query, {
    method: 'POST',
    body: form,
    headers: token ? { Authorization: `Bearer ${token}` } : {},
  })

  if (!response.ok) {
    const body = await response.json().catch(() => null)
    throw new ApiError(response.status, body?.title ?? 'Dosya yüklenemedi.', body?.errors)
  }

  return (await response.json()) as T
}

/**
 * Dosya indirme. Bağlantıyı doğrudan `<a href>` ile açamıyoruz: indirme ucu
 * jeton istiyor ve tarayıcı gezinme isteğine Authorization başlığı eklemiyor.
 * Bu yüzden içerik fetch ile alınıp geçici bir nesne URL'sinden kaydediliyor.
 */
async function download(path: string, fileName: string): Promise<void> {
  const token = tokenStore.get()

  const response = await fetch(path, {
    headers: token ? { Authorization: `Bearer ${token}` } : {},
  })

  if (!response.ok) {
    const body = await response.json().catch(() => null)
    throw new ApiError(response.status, body?.title ?? 'Dosya indirilemedi.')
  }

  const url = URL.createObjectURL(await response.blob())
  const link = document.createElement('a')
  link.href = url
  link.download = fileName
  document.body.appendChild(link)
  link.click()
  link.remove()

  // Serbest bırakma geciktiriliyor: tıklamadan hemen sonra iptal etmek büyük
  // dosyalarda indirmenin yarıda kesilmesine yol açabiliyor. Bırakılmazsa da
  // blob sekme kapanana kadar bellekte kalırdı.
  setTimeout(() => URL.revokeObjectURL(url), 60_000)
}

export const api = {
  get: <T>(path: string) => request<T>(path),
  upload,
  download,
  post: <T>(path: string, body?: unknown) =>
    request<T>(path, { method: 'POST', body: body ? JSON.stringify(body) : undefined }),
  put: <T>(path: string, body?: unknown) =>
    request<T>(path, { method: 'PUT', body: body ? JSON.stringify(body) : undefined }),
  del: <T>(path: string) => request<T>(path, { method: 'DELETE' }),
}
