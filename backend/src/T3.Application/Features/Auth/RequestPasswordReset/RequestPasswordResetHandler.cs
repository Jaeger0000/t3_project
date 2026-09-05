using System.Diagnostics;
using Microsoft.EntityFrameworkCore;
using T3.Application.Common.Interfaces;
using T3.Application.Common.Results;
using T3.Application.Common.Text;
using T3.Domain.Identity;

namespace T3.Application.Features.Auth.RequestPasswordReset;

/// <summary>
/// Şifre sıfırlama bağlantısı ister.
///
/// Bu uçtan önce sistemde şifre kurtarma <em>hiç yoktu</em>: tek yol yöneticinin
/// şifre atamasıydı, yani her hesabın şifresini bir başkası da biliyordu.
///
/// Uç herkese açık olduğu için iki kural pazarlık dışı: yanıt hiçbir zaman
/// e-postanın kayıtlı olup olmadığını söylemez ve hız sınırı giriş ucuyla aynı
/// kovada durur.
/// </summary>
public sealed class RequestPasswordResetHandler(
    IAppDbContext db,
    IEmailSender email,
    IClientContext client,
    IAuditWriter audit,
    IResetLinkBuilder linkBuilder)
{
    private const string SameAnswer =
        "Adres kayıtlıysa şifre sıfırlama bağlantısı gönderildi. E-postanızı kontrol edin.";

    /// <summary>
    /// Kayıtlı bir adres jeton üretimi + veritabanı yazımı + e-posta/dosya
    /// yazımı yapıyor; kayıtsız adres hiçbirini yapmıyordu — yanıt süresi
    /// adresin kayıtlı olup olmadığını ele veriyordu (bkz. G-05). Yanıt bu
    /// alt sınırın altına düşemez; gerçek iş daha uzun sürerse ek gecikme
    /// eklenmez.
    /// </summary>
    private static readonly TimeSpan MinimumResponseTime = TimeSpan.FromMilliseconds(200);

    public async Task<Result<RequestPasswordResetResponse>> Handle(
        RequestPasswordResetRequest request, CancellationToken ct)
    {
        var stopwatch = Stopwatch.StartNew();
        var result = await HandleCore(request, ct);

        var remaining = MinimumResponseTime - stopwatch.Elapsed;
        if (remaining > TimeSpan.Zero)
            await Task.Delay(remaining, ct);

        return result;
    }

    private async Task<Result<RequestPasswordResetResponse>> HandleCore(
        RequestPasswordResetRequest request, CancellationToken ct)
    {
        var normalized = SearchText.Normalize(request.Email);

        var user = await db.Users
            .FirstOrDefaultAsync(u => u.Email.ToLower() == normalized, ct);

        // Pasif hesaba da bağlantı gönderilmiyor: sıfırlama girişi geri
        // açmıyor, yöneticinin hesabı yeniden etkinleştirmesi gerekir.
        if (user is null || !user.IsActive)
        {
            await audit.WriteForActorAsync(
                actorUserId: null, actorRole: null,
                "Auth.PasswordResetRequested", nameof(User), null,
                after: new
                {
                    Email = MaskedEmail.Of(request.Email),
                    Result = user is null ? "kayıtlı olmayan adres" : "hesap pasif"
                },
                ct: ct);

            return new RequestPasswordResetResponse(SameAnswer);
        }

        // Daha önce verilmiş kullanılmamış bağlantılar geçersizleşiyor: aynı
        // hesap için birden fazla açık jeton, çalınan tek bağlantının ömrünü
        // gereksiz uzatır.
        var pending = await db.PasswordResetTokens
            .Where(t => t.UserId == user.Id && t.UsedAt == null)
            .ToListAsync(ct);

        var now = DateTimeOffset.UtcNow;

        foreach (var token in pending)
            token.UsedAt = now;

        var rawToken = PasswordResetSecrets.CreateRawToken();

        db.PasswordResetTokens.Add(new PasswordResetToken
        {
            UserId = user.Id,
            TokenHash = PasswordResetSecrets.Hash(rawToken),
            ExpiresAt = now.Add(PasswordResetSecrets.Lifetime),
            RequestedFromIp = client.IpAddress
        });

        await db.SaveChangesAsync(ct);

        var link = linkBuilder.Build(rawToken);

        // Gönderim hatası yanıtı değiştirmemeli. Kayıtsız adres 200 dönerken
        // kayıtlı adreste SMTP arızası 500 döndürseydi, uç "bu e-posta sistemde
        // var mı" sorusunu cevaplayan bir gösterge hâline gelirdi — sabit yanıt
        // ve sabit süre için ödenen bedel (bkz. G-05) boşa giderdi.
        //
        // Hata yutulmuyor: gönderici ERROR log'u düşürüyor ve denetim izine
        // "gönderim başarısız" yazılıyor, yani jüriye/operasyona görünür kalıyor.
        var delivered = true;

        try
        {
            await email.SendAsync(
                user.Email,
                "T3 Girişim Ekosistemi — şifre sıfırlama",
                $"""
                Sayın {user.FullName},

                Şifrenizi sıfırlamak için aşağıdaki bağlantıyı açın. Bağlantı
                {PasswordResetSecrets.Lifetime.TotalHours:0} saat geçerlidir ve yalnızca
                bir kez kullanılabilir.

                {link}

                Bu isteği siz yapmadıysanız bir şey yapmanıza gerek yok; şifreniz
                değişmedi.
                """,
                ct);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            delivered = false;
        }

        // İzde ham e-posta ve jeton yok: olayın kendisi kayıtlı, sırrın ikinci
        // kopyası üretilmiyor.
        await audit.WriteForActorAsync(
            actorUserId: null, actorRole: null,
            "Auth.PasswordResetRequested", nameof(User), user.Id,
            after: new
            {
                Email = MaskedEmail.Of(user.Email),
                Result = delivered ? "bağlantı gönderildi" : "gönderim başarısız"
            },
            ct: ct);

        return new RequestPasswordResetResponse(SameAnswer);
    }
}
