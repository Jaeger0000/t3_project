namespace T3.Domain.Identity;

/// <summary>Problem 7 brifinde tanımlı dört rol.</summary>
public enum UserRole
{
    /// <summary>Tüm girişim, program, kullanıcı ve onay süreçlerini yönetir.</summary>
    SuperAdmin = 1,

    /// <summary>Yalnızca atandığı programlardaki girişimleri yönetir.</summary>
    ProgramManager = 2,

    /// <summary>Yalnızca kendi girişimini görür; değişiklikleri onaya düşer.</summary>
    StartupUser = 3,

    /// <summary>Salt okunur; finansalları yalnızca agregat düzeyde görür.</summary>
    DecisionMaker = 4
}
