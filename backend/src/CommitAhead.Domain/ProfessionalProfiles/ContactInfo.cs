namespace CommitAhead.Domain.ProfessionalProfiles;

/// <summary>Global identity and contact data held on the ProfessionalProfile — never duplicated on a CVPresentation (CONTEXT.md).</summary>
public sealed class ContactInfo
{
    public string Name { get; }
    public string Email { get; }
    public string? Phone { get; }
    public string? Address { get; }
    public string? PhotoStorageKey { get; }

    public ContactInfo(string name, string email, string? phone, string? address, string? photoStorageKey)
    {
        Name = TextValidation.RequireNonBlank(name, nameof(name), ValidationLimits.ShortTextMaxLength);
        Email = TextValidation.RequireNonBlank(email, nameof(email), ValidationLimits.EmailMaxLength);
        Phone = TextValidation.TrimToNullOrValidate(phone, nameof(phone), ValidationLimits.ShortTextMaxLength);
        Address = TextValidation.TrimToNullOrValidate(address, nameof(address), ValidationLimits.ShortTextMaxLength);
        PhotoStorageKey = TextValidation.TrimToNullOrValidate(photoStorageKey, nameof(photoStorageKey), ValidationLimits.ShortTextMaxLength);
    }
}
