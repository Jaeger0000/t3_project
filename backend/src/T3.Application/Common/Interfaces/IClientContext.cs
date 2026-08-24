namespace T3.Application.Common.Interfaces;

/// <summary>
/// İsteğin nereden geldiği. <see cref="ICurrentUser"/> "kim" sorusunun cevabı,
/// bu arayüz "nereden" sorusunun — ikisi ayrı duruyor çünkü ağ bilgisi kimlik
/// değil ve MCP gibi HTTP dışı yollarda boş kalması normal.
///
/// Denetim izinin tek tüketicisi bu bilgi: KVKK incelemesinde "bu kaydı kim
/// değiştirdi" sorusunun yanında "hangi adresten" sorusu da cevaplanmalı.
/// </summary>
public interface IClientContext
{
    /// <summary>İstemci IP adresi; ters vekil arkasında X-Forwarded-For dikkate alınır.</summary>
    string? IpAddress { get; }

    /// <summary>Tarayıcı/istemci kimliği. Uzunluk denetim kolonunda kırpılır.</summary>
    string? UserAgent { get; }
}
