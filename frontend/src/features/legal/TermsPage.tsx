import { useDocumentTitle } from '@/lib/useDocumentTitle'
import LegalDocument, { LegalSection } from './LegalDocument'

export default function TermsPage() {
  useDocumentTitle('Kullanım şartları')

  return (
    <LegalDocument title="Kullanım şartları" updatedOn="24 Ağustos 2026">
      <LegalSection heading="Kapsam">
        <p>
          Bu sistem T3 Vakfı girişimcilik ekosisteminin yönetimi için kurum içi
          kullanıma açıktır. Hesaplar sistem yöneticisi tarafından oluşturulur;
          herkese açık kayıt yoktur.
        </p>
      </LegalSection>

      <LegalSection heading="Hesap güvenliği">
        <p>
          Hesabınızla yapılan işlemlerden siz sorumlusunuz. Şifrenizi
          paylaşmayın; yönetici tarafından atanan şifreyi ilk girişte
          değiştirmeniz zorunludur. Şifrenizin ele geçirildiğini düşünüyorsanız
          derhâl değiştirin ve sistem yöneticisine bildirin.
        </p>
      </LegalSection>

      <LegalSection heading="Veri doğruluğu ve onay akışı">
        <p>
          Girişim kullanıcıları profil bilgilerini doğrudan değiştirmez; gönderdiği
          değişiklikler program ekibinin onayından geçer. Girdiğiniz bilgilerin
          doğruluğundan siz sorumlusunuz; yanlış bilgi program değerlendirmelerini
          etkiler.
        </p>
      </LegalSection>

      <LegalSection heading="Yetkisiz kullanım">
        <p>
          Yetkiniz dışındaki verilere erişmeye çalışmak, sistemi otomatik araçlarla
          taramak veya elde ettiğiniz verileri kurum dışına çıkarmak yasaktır.
          Tüm okuma ve yazma işlemleri denetim izine kaydedilir.
        </p>
      </LegalSection>

      <LegalSection heading="Yapay zekâ destekli özellikler">
        <p>
          Soru-cevap paneli karar <em>destek</em> katmanıdır, karar verici
          değildir. Yanıtlar yalnızca sizin görme yetkiniz olan kayıtlardan
          üretilir ve her yanıtın hangi kayıtlardan geldiği ekranda gösterilir.
          Yanıtları resmî rapor olarak kullanmadan önce kaynak kayıtlardan
          doğrulayın.
        </p>
      </LegalSection>

      <LegalSection heading="Kesinti ve değişiklik">
        <p>
          Sistem, bakım ve geliştirme nedeniyle geçici olarak kullanılamayabilir.
          Şartlarda yapılan değişiklikler bu sayfada yayımlanır.
        </p>
      </LegalSection>
    </LegalDocument>
  )
}
