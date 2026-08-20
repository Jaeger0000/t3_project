using T3.Domain.Startups;

namespace T3.Application.Common.Rbac;

/// <summary>
/// Girişim sorgularını çağıran kullanıcının yetki kapsamına daraltır.
/// Tek uygulama noktası olması kritik: her handler bunu çağırır, kimse
/// kendi filtresini yazmaz.
/// </summary>
public interface IStartupScope
{
    IQueryable<Startup> Apply(IQueryable<Startup> query);

    /// <summary>Kullanıcı bu girişimi doğrudan düzenleyebilir mi (onay akışı olmadan)?</summary>
    bool CanEditDirectly(Guid startupId);

    /// <summary>Kullanıcı bu girişim için gelen onay isteklerini karara bağlayabilir mi?</summary>
    bool CanReviewApprovals { get; }
}
