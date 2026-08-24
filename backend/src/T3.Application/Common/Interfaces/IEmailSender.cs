namespace T3.Application.Common.Interfaces;

/// <summary>
/// E-posta gönderimi. Creathon kapsamında gerçek SMTP kurulmadığı için tek
/// uygulaması geliştirme kutusuna (dosya + log) yazıyor; arayüz yine de burada
/// duruyor çünkü şifre sıfırlama akışı sağlayıcıya bağlı olmamalı — SMTP
/// geldiğinde yalnızca kayıt satırı değişir.
/// </summary>
public interface IEmailSender
{
    Task SendAsync(string to, string subject, string body, CancellationToken ct = default);
}
