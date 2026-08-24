using T3.Domain.Approvals;

namespace T3.Application.Features.Approvals;

/// <summary>
/// Onay isteklerinin satır düzeyi kapsamı. <c>IStartupScope</c> ile aynı
/// gerekçe: tek uygulama noktası olsun, hiçbir dilim kendi filtresini yazmasın.
/// </summary>
public interface IChangeRequestScope
{
    IQueryable<ChangeRequest> Apply(IQueryable<ChangeRequest> query);

    /// <summary>Kullanıcı kendi adına onay isteği gönderebilir mi?</summary>
    bool CanSubmit { get; }

    /// <summary>Kullanıcı gelen istekleri karara bağlayabilir mi?</summary>
    bool CanReview { get; }
}
