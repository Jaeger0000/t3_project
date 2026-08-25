namespace T3.Application.Features.Auth.Login;

/// <param name="KvkkConsentVersion">
/// Giriş ekranında onaylanan aydınlatma metninin sürümü. Bilinçli olarak
/// <b>zorunlu değil</b>: giriş ekranını hiç görmeyen istemciler (betikler, MCP,
/// Swagger) aynı uçtan geçiyor ve alanı zorunlu kılmak onları kırardı. Değer
/// geldiğinde denetim izine ayrı bir satır düşüyor — onayın kanıtı orası.
/// </param>
public sealed record LoginRequest(string Email, string Password, string? KvkkConsentVersion = null);
