using T3.Domain.Documents;

namespace T3.Application.Features.Documents;

/// <summary>
/// Onay bekleyen doküman yüklemesinin gövdesi.
///
/// Dosya, öneri gönderildiği anda depoya yazılır; kayıt satırı ancak onaydan
/// sonra oluşur. İçeriği onaya kadar istemcide tutmak mümkün değil (yükleme tek
/// seferlik), veritabanında tutmak ise dosyayı veritabanına taşımak olurdu.
/// <c>StoragePath</c> bu yüzden gövdenin parçası: onaylayan handler dosyayı
/// bulup kaydı oluşturur, reddeden handler dosyayı siler.
/// </summary>
public sealed record DocumentProposalModel(
    DocumentType Type,
    string FileName,
    string StoragePath,
    string ContentType,
    long SizeBytes);
