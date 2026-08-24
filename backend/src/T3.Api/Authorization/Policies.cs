namespace T3.Api.Authorization;

/// <summary>
/// Politika adları. Uç noktalar bu sabitlerle işaretlenir; rol listeleri
/// Program.cs'te tek yerde tanımlanır. Satır düzeyi daraltma her zaman
/// ayrıca IStartupScope ile yapılır — politika yalnızca kaba yetki kapısıdır.
/// </summary>
public static class Policies
{
    public const string ManageStartups = "startups:manage";

    /// <summary>
    /// Program tanımı — yalnızca SuperAdmin. Program listesi aynı zamanda
    /// Program Yöneticisi'nin yetki kapsamının tanımı olduğu için, kendi
    /// kapsamını büyütebilen bir rol RBAC'ı anlamsız kılardı.
    /// </summary>
    public const string ManagePrograms = "programs:manage";

    /// <summary>
    /// Dönem ve katılım işlemleri — günlük operasyon, Program Yöneticisi'ne de
    /// açık. Hangi programda olduğu satır düzeyinde ProgramAccessGuard ile
    /// daraltılır.
    /// </summary>
    public const string ManageProgramTerms = "program-terms:manage";
    public const string ReviewApprovals = "approvals:review";
    public const string ManageUsers = "users:manage";
    public const string ViewAuditLogs = "audit:view";
}
