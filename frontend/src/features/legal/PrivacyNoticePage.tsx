import { useDocumentTitle } from '@/lib/useDocumentTitle'
import LegalDocument, { LegalSection } from './LegalDocument'

export default function PrivacyNoticePage() {
  useDocumentTitle('KVKK aydınlatma metni')

  return (
    <LegalDocument title="KVKK aydınlatma metni" updatedOn="24 Ağustos 2026">
      <LegalSection heading="Veri sorumlusu">
        <p>
          T3 Vakfı (Türkiye Teknoloji Takımı Vakfı), 6698 sayılı Kişisel Verilerin
          Korunması Kanunu kapsamında veri sorumlusudur. T3 Girişim Ekosistemi
          Yönetim Sistemi, vakfın girişimcilik programlarının kurumsal hafızasını
          tutmak için kullanılır.
        </p>
      </LegalSection>

      <LegalSection heading="İşlenen kişisel veriler">
        <ul className="list-disc pl-5">
          <li>
            <strong>Kimlik ve iletişim:</strong> ad soyad, e-posta adresi, telefon,
            görev/unvan, LinkedIn adresi.
          </li>
          <li>
            <strong>Hesap verisi:</strong> rol, yetki kapsamı (program/girişim),
            son giriş zamanı, şifre özeti (şifrenin kendisi saklanmaz).
          </li>
          <li>
            <strong>İşlem güvenliği:</strong> giriş denemeleri, IP adresi, istemci
            bilgisi ve yapılan değişikliklerin denetim kaydı.
          </li>
          <li>
            <strong>Girişim verisi:</strong> girişim profili, program geçmişi,
            başarı/finans kayıtları, yüklenen dokümanlar. Bu veriler tüzel kişiye
            aittir; ekip üyesi bilgileri kişisel veri olarak ele alınır.
          </li>
        </ul>
      </LegalSection>

      <LegalSection heading="İşleme amaçları ve hukuki sebep">
        <p>
          Veriler; başvuru ve program süreçlerinin yürütülmesi, girişimlerin
          gelişiminin izlenmesi, raporlama ve karar desteği, yetkisiz erişimin
          önlenmesi ve yasal yükümlülüklerin yerine getirilmesi amacıyla işlenir.
          Hukuki sebep, sözleşmenin kurulması/ifası ve veri sorumlusunun meşru
          menfaatidir; program başvurusu sırasında verilen açık rıza saklıdır.
        </p>
      </LegalSection>

      <LegalSection heading="Erişim ve rol bazlı sınırlama">
        <p>
          Sistem, verileri rol bazlı maskeler. Program Yöneticisi yalnızca
          sorumlu olduğu programlardan geçmiş girişimleri görür; Karar Verici
          finansal tutarları yalnızca toplulaştırılmış biçimde ve iletişim
          bilgilerini hiç görmez; girişim kullanıcısı yalnızca kendi girişimini
          görür ve doğrudan yazamaz — değişiklikleri onaya düşer. Her okuma ve
          yazma yetkisi sunucu tarafında ayrıca denetlenir.
        </p>
      </LegalSection>

      <LegalSection heading="Aktarım">
        <p>
          Kişisel veriler üçüncü taraflarla paylaşılmaz; yurt dışına aktarılmaz.
          Yapay zekâ destekli soru-cevap özelliği kullanıldığında yalnızca soruyu
          soran kullanıcının görme yetkisi olan kayıtlar model sağlayıcısına
          gönderilir; bu özellik kapatılabilir ve anahtar tanımlı değilken
          sistem tamamen yerel çalışır.
        </p>
      </LegalSection>

      <LegalSection heading="Saklama süresi">
        <p>
          Kayıtlar program ilişkisi sürdüğü ve yasal saklama süreleri boyunca
          tutulur. Silinen kayıtlar "pasife alınır": denetim izinin bütünlüğü
          için kayıt tarihçesi korunur, veriler aktif ekranlarda görünmez.
        </p>
      </LegalSection>

      <LegalSection heading="Haklarınız ve başvuru">
        <p>
          KVKK m.11 uyarınca kişisel verilerinizin işlenip işlenmediğini
          öğrenme, bilgi talep etme, düzeltilmesini veya silinmesini isteme,
          işlemeye itiraz etme ve zararınızın giderilmesini talep etme
          haklarına sahipsiniz. Başvurularınızı{' '}
          <a
            href="mailto:kvkk@t3vakfi.org.tr"
            className="text-brand-700 hover:underline dark:text-brand-200"
          >
            kvkk@t3vakfi.org.tr
          </a>{' '}
          adresine iletebilirsiniz. Girişim portalı kullanıcıları düzeltme
          taleplerini sistem içindeki onay akışı üzerinden de gönderebilir.
        </p>
      </LegalSection>

      <LegalSection heading="Tarayıcıda saklanan veriler">
        <p>
          Sistem takip çerezi kullanmaz. Oturumun sürmesi için erişim jetonu
          tarayıcınızın yerel deposunda tutulur; çıkış yaptığınızda silinir.
        </p>
      </LegalSection>
    </LegalDocument>
  )
}
