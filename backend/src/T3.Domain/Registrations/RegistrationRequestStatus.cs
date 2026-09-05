namespace T3.Domain.Registrations;

/// <summary>Girişim kendi kendine kayıt başvurusunun yaşam döngüsü.</summary>
public enum RegistrationRequestStatus
{
    Pending = 1,
    Approved = 2,
    Rejected = 3
}
