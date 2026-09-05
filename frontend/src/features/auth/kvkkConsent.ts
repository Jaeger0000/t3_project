/**
 * Giriş ekranındaki KVKK onayının istemci tarafı.
 *
 * Neden sürüm numarası: onay "bir kez tıklandı" değil "şu metne verildi"
 * demektir. Aydınlatma metni değiştiğinde sürüm artırılır ve kullanıcıya
 * yeniden sorulur — eski onayı yeni metne saymak KVKK açısından onay değil
 * varsayım olurdu.
 *
 * Neden `localStorage`: buradaki kayıt yalnızca "bu tarayıcıda bir daha
 * sormayalım" kolaylığı. Kanıt niteliği taşıyan kayıt sunucuda, denetim izinde
 * duruyor (`Auth.KvkkConsent`); istemcideki değeri kullanıcı silebilir ve bu
 * bilinçli olarak sorun değil.
 */
export const KVKK_ONAY_SURUMU = '2026-09-04'

const ANAHTAR = 't3.kvkk.onay'

type KayitliOnay = { surum: string; tarih: string }

/** Bu tarayıcıda geçerli sürüm için onay kaydı var mı? */
export function kvkkOnayiniOku(): KayitliOnay | null {
  try {
    const ham = localStorage.getItem(ANAHTAR)
    if (!ham) return null

    const kayit = JSON.parse(ham) as Partial<KayitliOnay>
    if (kayit.surum !== KVKK_ONAY_SURUMU || !kayit.tarih) return null

    return { surum: kayit.surum, tarih: kayit.tarih }
  } catch {
    // Bozuk/elle düzenlenmiş değer: onay yok sayılır, kullanıcıya yeniden sorulur.
    return null
  }
}

/** Onayı bu tarayıcıya yazar; sunucu kaydı girişle birlikte oluşuyor. */
export function kvkkOnayiniYaz(): void {
  const kayit: KayitliOnay = {
    surum: KVKK_ONAY_SURUMU,
    tarih: new Date().toISOString(),
  }

  try {
    localStorage.setItem(ANAHTAR, JSON.stringify(kayit))
  } catch {
    // Depolama kapalıysa (gizli sekme kotası) onay yine geçerli: giriş isteği
    // sürümü sunucuya taşıyor, kullanıcı yalnızca bir dahaki gelişinde tekrar işaretler.
  }
}
