namespace T3.Application.Common.Interfaces;

public interface IPasswordHasher
{
    string Hash(string password);
    bool Verify(string password, string hash);

    /// <summary>
    /// <see cref="Verify"/> ile aynı doğrulamayı yapar, ek olarak hash'in
    /// güncel parametrelerle (iterasyon sayısı) üretilip üretilmediğini
    /// söyler. İterasyon sayısı zamanla yükseltildiğinde eski girişlerde
    /// üretilmiş hash'ler bu sinyal kullanılmadan hiç güncellenmez — parola
    /// hâlâ eski (daha zayıf) parametrelerle korunmuş kalır (bkz. G-15,
    /// Guvenlik_Denetimi_ve_Iyilestirme_Plani.md). <paramref name="rehashedHash"/>
    /// yükseltme gerekiyorsa dolu döner; çağıran bunu kalıcı hâle getirmekle
    /// yükümlü (yalnızca doğru şifre girildiğinde çalışır, düz metin zaten elde).
    /// </summary>
    bool VerifyAndGetRehash(string password, string hash, out string? rehashedHash);

    /// <summary>
    /// Kayıtlı olmayan bir e-postayla giriş denendiğinde bu hash'e karşı
    /// doğrulama çalıştırılır: PBKDF2'nin süresi asıl maliyet, "kullanıcı yok"
    /// yolu bunu atlarsa yanıt süresi kayıtlı adresi ele verir (zamanlama yan
    /// kanalı — bkz. G-05, Guvenlik_Denetimi_ve_Iyilestirme_Plani.md).
    /// </summary>
    string DummyHashForTiming { get; }
}
