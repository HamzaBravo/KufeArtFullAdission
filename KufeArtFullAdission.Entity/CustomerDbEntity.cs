namespace KufeArtFullAdission.Entity;

public sealed class CustomerDbEntity:BaseDbEntity
{
    public string Fullname { get; set; }
    public string PhoneNumber { get; set; }
    public string Password { get; set; } // ✅ YENİ: Şifre alanı
    public bool IsActive { get; set; }
}
