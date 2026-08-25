namespace T3.Api.Hosting;

/// <summary>
/// Derlenmiş arayüzü API'nin kendisi sunar.
///
/// Bu karar önceki durumun bir hatasını kapatıyor: istemci tüm isteklerini
/// göreli <c>/api/...</c> yoluna atıyordu ve o yolu backend'e taşıyan tek şey
/// Vite'ın <em>geliştirme</em> sunucusundaki vekildi. Yani <c>npm run build</c>
/// çıktısı hiçbir API'ye ulaşamıyordu.
///
/// Seçilen yol tek origin: CORS yok, vekil yok, çerez <c>SameSite=Strict</c>
/// kalabiliyor (bkz. <see cref="Security.SessionCookie"/>). Alternatif olan
/// "nginx + ayrı konteyner" kurulumu aynı sonucu üretir ama iki imaj ve bir
/// konfigürasyon dosyası daha ister; jüriye tek komutla kalkan bir yığın
/// göstermek daha değerli.
/// </summary>
public static class SpaHosting
{
    /// <summary>
    /// Statik dosya katmanı. Dosyalar herkese açık: kimlik kontrolü uygulama
    /// içinde, indirilen belgeler ayrı uçta ve jetonlu.
    /// </summary>
    public static bool UseCompiledFrontend(this WebApplication app)
    {
        if (!HasCompiledFrontend(app))
        {
            // Geliştirmede beklenen durum (arayüz Vite'ta çalışıyor); üretimde
            // sessiz kalmak "site açılmıyor" hatasını teşhis edilemez yapardı.
            if (!app.Environment.IsDevelopment())
                app.Logger.LogWarning(
                    "Derlenmiş arayüz bulunamadı ({Root}) — yalnızca API sunuluyor.",
                    app.Environment.WebRootPath);

            return false;
        }

        app.UseStaticFiles(new StaticFileOptions
        {
            OnPrepareResponse = context =>
            {
                var path = context.File.Name;

                // Vite dosya adına içerik özeti koyuyor: adı değişmeden içeriği
                // değişemez, o yüzden sonsuza kadar önbelleklenebilir.
                // index.html'de özet yok — her açılışta doğrulanmalı, yoksa
                // dağıtımdan sonra eski paket adlarını isteyen bir sayfa kalır.
                context.Context.Response.Headers.CacheControl =
                    path.Equals("index.html", StringComparison.OrdinalIgnoreCase)
                        ? "no-cache"
                        : "public, max-age=31536000, immutable";
            }
        });

        return true;
    }

    /// <summary>
    /// SPA geri dönüşü: tarayıcı adres çubuğundan gelen <c>/girisimler/…</c>
    /// gibi istemci rotaları index.html'e düşer.
    ///
    /// API önekleri bilinçli olarak dışarıda: <c>/api/olmayan-uc</c> için
    /// index.html döndürmek 404 sözleşmesini bozar — istemci JSON beklerken
    /// HTML ayrıştırmaya çalışır ve hata mesajı anlamsızlaşır.
    /// </summary>
    public static void MapSpaFallback(this WebApplication app)
    {
        if (!HasCompiledFrontend(app))
            return;

        app.MapFallback(async context =>
        {
            if (IsBackendPath(context.Request.Path))
            {
                // Gövdeyi UseStatusCodePages tek biçimli JSON'a çeviriyor.
                context.Response.StatusCode = StatusCodes.Status404NotFound;
                return;
            }

            context.Response.ContentType = "text/html; charset=utf-8";
            context.Response.Headers.CacheControl = "no-cache";
            await context.Response.SendFileAsync(IndexPath(app));
        }).AllowAnonymous();
    }

    private static bool IsBackendPath(PathString path) =>
        path.StartsWithSegments("/api")
        || path.StartsWithSegments("/health")
        || path.StartsWithSegments("/mcp")
        || path.StartsWithSegments("/swagger");

    private static bool HasCompiledFrontend(WebApplication app) =>
        !string.IsNullOrEmpty(app.Environment.WebRootPath) && File.Exists(IndexPath(app));

    private static string IndexPath(WebApplication app) =>
        Path.Combine(app.Environment.WebRootPath ?? string.Empty, "index.html");
}
