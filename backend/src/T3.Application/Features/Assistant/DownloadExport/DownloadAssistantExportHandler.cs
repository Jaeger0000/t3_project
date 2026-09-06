using T3.Application.Common.Interfaces;
using T3.Application.Common.Results;

namespace T3.Application.Features.Assistant.DownloadExport;

/// <summary>
/// AI asistanının araç turunda ürettiği dosyayı (ör. Excel dışa aktarma)
/// indirir. Jeton tek kullanımlık ve sahibine özel — bkz. IAssistantExportStore.
/// Politika hattı bu ucu zaten kapatıyor (bkz. AssistantEndpoints); kontrol
/// burada da duruyor çünkü MCP araçları handler'ları doğrudan çağırabiliyor.
/// </summary>
public sealed class DownloadAssistantExportHandler(
    IAssistantExportStore store,
    ICurrentUser currentUser)
{
    public Result<AssistantExportFile> Handle(string token)
    {
        if (currentUser.UserId is not { } userId)
            return Error.Forbidden("İndirme için oturum açmalısınız.");

        var file = store.Take(userId, token);

        // "Bulunamadı" hem yanlış jeton hem başkasının jetonu hem de zaten
        // kullanılmış/süresi dolmuş jeton için aynı: hangisi olduğunu ayırt
        // etmek saldırgana jeton tahmin etmesi için geri bildirim verirdi.
        return file is null
            ? Error.NotFound("Bağlantının süresi dolmuş ya da zaten kullanılmış.")
            : file;
    }
}
