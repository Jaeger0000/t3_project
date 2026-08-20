namespace T3.Api.Authorization;

/// <summary>
/// Politika adları. Uç noktalar bu sabitlerle işaretlenir; rol listeleri
/// Program.cs'te tek yerde tanımlanır. Satır düzeyi daraltma her zaman
/// ayrıca IStartupScope ile yapılır — politika yalnızca kaba yetki kapısıdır.
/// </summary>
public static class Policies
{
    public const string ManageStartups = "startups:manage";
    public const string ReviewApprovals = "approvals:review";
    public const string ManageUsers = "users:manage";
}
