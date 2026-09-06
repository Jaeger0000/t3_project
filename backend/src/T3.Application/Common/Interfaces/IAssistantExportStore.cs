namespace T3.Application.Common.Interfaces;

/// <summary>İndirilebilir bir AI çıktısı. bkz. <see cref="IAssistantExportStore"/>.</summary>
public sealed record AssistantExportFile(string FileName, string ContentType, byte[] Content);

/// <summary>
/// Sohbet protokolü ikili veri taşıyamaz: araç modele yalnızca metin/JSON
/// döndürebilir. Bu yüzden asistanın ürettiği dosyalar (ör. Excel dışa
/// aktarma) burada kısa süreliğine bekletilir; araç modele yalnızca bir jeton
/// döner, istemci jetonu ayrı bir GET ucuna sunup dosyayı indirir.
///
/// Jeton tek kullanımlık ve yalnızca üreten kullanıcıya açık — başka bir
/// kullanıcı aynı jetonu denerse (jetonu bir şekilde ele geçirmiş olsa bile)
/// bulunamadı döner, çünkü sahiplik kontrolü <see cref="Take"/> içinde.
/// </summary>
public interface IAssistantExportStore
{
    string Save(Guid ownerUserId, AssistantExportFile file);

    AssistantExportFile? Take(Guid ownerUserId, string token);
}
