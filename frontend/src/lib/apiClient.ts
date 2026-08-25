/**
 * Tek HTTP giriş noktası. Kimlik taşıma ve hata normalleştirme burada durur;
 * özellik klasörleri yalnızca `api.get/post` çağırır.
 *
 * Jeton artık JavaScript'in elinde değil: sunucu onu `HttpOnly` çereze yazıyor
 * (bkz. backend `SessionCookie`). Bu dosyada bilinçli olarak *hiçbir* jeton
 * saklama kodu yok — `localStorage`'daki jeton tek bir XSS ile okunabiliyordu
 * ve Süper Yönetici oturumu 32 girişimin vergi numarasına, iletişim bilgisine
 * ve finansallarına açılıyordu.
 */

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

/**
 * Ağ hatasının tek metni. API kapalıyken tarayıcı "Failed to fetch" fırlatıyor
 * ve bu ham metin kullanıcının ekranına düşüyordu; ayrıca `fetch` reddi bir
 * yetki sorunu gibi ele alınıp oturumu düşürüyordu. Ağ hatası artık status 0
 * taşıyan bir ApiError: çağıran taraf "sunucuya ulaşılamıyor" ile "yetkin yok"
 * arasında ayrım yapabiliyor.
 */
export const NETWORK_ERROR_MESSAGE =
  'Sunucuya ulaşılamıyor. Bağlantınızı kontrol edip yeniden deneyin.'

async function send(path: string, init: RequestInit): Promise<Response> {
  try {
    return await fetch(path, init)
  } catch {
    // Yakalanan tek durum ağ/CORS kaynaklı reddetme; HTTP hata kodları
    // `fetch`'i reddetmez, aşağıda status'e göre ele alınıyor.
    throw new ApiError(0, NETWORK_ERROR_MESSAGE)
  }
}

const CSRF_COOKIE = 't3.csrf'
const CSRF_HEADER = 'X-CSRF-Token'

/**
 * CSRF çift-gönderim jetonu. Sunucu onu okunabilir bir çereze yazıyor; buradaki
 * tek iş onu geri başlığa koymak. Değer sır değil — kanıtladığı şey isteğin
 * *bizim sayfamızda* kurulduğu, çünkü başka bir origin bu çerezi okuyamaz.
 */
function csrfToken(): string | null {
  const match = document.cookie.match(new RegExp(`(?:^|; )${CSRF_COOKIE}=([^;]*)`))
  return match ? decodeURIComponent(match[1]) : null
}

/** Durum değiştiren yöntemler CSRF başlığı taşır; okumalar taşımaz. */
function csrfHeaders(method: string | undefined): Record<string, string> {
  if (!method || method === 'GET' || method === 'HEAD') return {}
  const token = csrfToken()
  return token ? { [CSRF_HEADER]: token } : {}
}

async function request<T>(path: string, init: RequestInit = {}): Promise<T> {
  const response = await send(path, {
    ...init,
    // Kimlik çerezde: `same-origin` tek origin kurulumunda çerezi gönderir,
    // üçüncü taraf bir adrese asla göndermez.
    credentials: 'same-origin',
    headers: {
      'Content-Type': 'application/json',
      ...csrfHeaders(init.method),
      ...init.headers,
    },
  })

  if (response.status === 401) {
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
  const form = new FormData()
  form.append('file', file)

  const response = await send(path + query, {
    method: 'POST',
    body: form,
    credentials: 'same-origin',
    headers: csrfHeaders('POST'),
  })

  if (!response.ok) {
    const body = await response.json().catch(() => null)
    throw new ApiError(response.status, body?.title ?? 'Dosya yüklenemedi.', body?.errors)
  }

  return (await response.json()) as T
}

/**
 * Dosya indirme. Jeton çereze taşındıktan sonra `<a href>` de çalışırdı ama
 * fetch yolu korunuyor: hata durumunda sunucunun JSON gövdesini okuyup Türkçe
 * mesaj gösterebiliyoruz, gezinme isteğinde tarayıcı ham hata sayfası basardı.
 */
async function download(path: string, fileName: string): Promise<void> {
  const response = await send(path, { credentials: 'same-origin' })

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
