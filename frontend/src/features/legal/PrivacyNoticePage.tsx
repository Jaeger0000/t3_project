import { useDocumentTitle } from '@/lib/useDocumentTitle'
import LegalDocument, { LegalSection } from './LegalDocument'

export default function PrivacyNoticePage() {
  useDocumentTitle('KVKK aydınlatma metni')

  return (
    <LegalDocument title="KVKK aydınlatma metni" updatedOn="4 Eylül 2026">
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
            <strong>Teknik kayıt:</strong> her isteğe ait IP adresi, istek
            kimliği, tarayıcı bilgisi ve yöntem/yol/süre özeti. Bu kayıtlar
            hataları teşhis etmek için tutulur; e-posta, telefon, vergi
            numarası ve tutar gibi alanlar bu kayda ham hâliyle yazılmaz.
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
          Kişisel veriler üçüncü taraflarla paylaşılmaz. Yapay zekâ destekli
          soru-cevap özelliği bir yönetici tarafından etkin bırakıldığında (bir
          model erişim anahtarı tanımlıysa) soruyu soran kullanıcının kendi
          yetkisiyle görebildiği kayıtlar yanıtı üretmek amacıyla yurt dışında
          yerleşik model sağlayıcısına (Anthropic) gönderilir — ama bu kayıtlar
          önce ayrı bir süzgeçten geçer: ad soyad, e-posta, telefon, LinkedIn
          adresi ve vergi numarası gibi kişiye ya da girişime özel kimlik
          bilgileri modele gitmeden çıkarılır. Giden veri girişimin adı, sektörü,
          şehri, program geçmişi ve toplulaştırılmış sayılarla sınırlıdır.
          Bu daraltılmış aktarım da KVKK m.9 kapsamında bir <strong>yurt dışına
          aktarımdır</strong> ve aktarım mekanizması ile veri işleyen ilişkisi ayrı
          bir süreçte kurulur. Anahtar tanımlı değilken (varsayılan durum)
          hiçbir veri sistem dışına çıkmaz; sorular tamamen yerel bir plana göre
          yanıtlanır ve ekranda hangi modun yanıtladığı açıkça yazar.
        </p>
      </LegalSection>

      <LegalSection heading="Saklama süresi">
        <p>
          Kayıtlar program ilişkisi sürdüğü ve yasal saklama süreleri boyunca
          tutulur. Silinen kayıtlar "pasife alınır": denetim izinin bütünlüğü
          için kayıt tarihçesi korunur, veriler aktif ekranlarda görünmez.
          Teknik kayıtlar (yukarıdaki madde) yalnızca 14 gün tutulur ve bu
          sürenin sonunda otomatik olarak silinir. Denetim izi kayıtları
          (kim, ne zaman, neyi değiştirdi) oluşturulmalarından itibaren 10 yıl
          saklanır — Türk Ticaret Kanunu'nun defter/belge saklama süresine
          kıyasen belirlenmiş bir üst sınırdır — ve bu sürenin sonunda
          anonimleştirilir. Yapay zekâ sohbet geçmişi (sorduğunuz sorular ve
          aldığınız yanıtlar) son mesajdan itibaren 1 yıl saklanır ve bu sürenin
          sonunda pasife alınmaz, <strong>tümüyle silinir</strong> — serbest
          metin olduğu için anonimleştirilecek bir alan yoktur. Sohbetlerinizi
          yalnızca siz görürsünüz. Şifre sıfırlama bağlantıları en fazla 2 saat
          geçerlidir ve kullanımdan veya süre dolumundan sonra silinir.
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
          Sistem takip veya reklam amaçlı çerez kullanmaz. Oturumun sürmesi
          için iki zorunlu çerez yazılır — bunlar açık rıza gerektirmeyen,
          hizmetin çalışması için gerekli teknik çerezlerdir:
        </p>
        <ul className="list-disc pl-5">
          <li>
            <strong>t3.session:</strong> erişim jetonunuzu taşır. Tarayıcı
            betiklerinin erişemediği (<code>HttpOnly</code>) ve yalnızca
            HTTPS üzerinden gönderilen bir çerezdir; oturum kapatıldığında
            veya süresi dolduğunda geçersiz kılınır.
          </li>
          <li>
            <strong>t3.csrf:</strong> oturum çerezinizle yapılan yazma
            isteklerinin başka bir siteden tetiklenmediğini doğrulamak için
            kullanılır, kişisel veri taşımaz.
          </li>
        </ul>
      </LegalSection>
    </LegalDocument>
  )
}
