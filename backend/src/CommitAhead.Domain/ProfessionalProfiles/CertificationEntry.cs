using CommitAhead.Domain;

namespace CommitAhead.Domain.ProfessionalProfiles;

/// <summary>A canonical professional certification inside a ProfessionalProfile (CONTEXT.md).</summary>
public sealed class CertificationEntry
{
    public Guid Id { get; }
    public string Name { get; }
    public string IssuingOrganisation { get; }
    public YearMonth? IssuedAt { get; }
    public YearMonth? ExpiresAt { get; }
    public string? CredentialId { get; }
    public string? Url { get; }

    // Assigned by ProfessionalProfile.ReplaceCertifications from the caller's array order — not a
    // constructor parameter, since it's aggregate-managed persistence state, not something a
    // caller building one entry in isolation should have to supply or could get wrong.
    public int Position { get; internal set; }

    public CertificationEntry(
        Guid id,
        string name,
        string issuingOrganisation,
        YearMonth? issuedAt,
        YearMonth? expiresAt,
        string? credentialId,
        string? url)
    {
        if (id == Guid.Empty)
        {
            throw new DomainValidationException("Id is required.");
        }

        Id = id;
        Name = TextValidation.RequireNonBlank(name, nameof(name), ValidationLimits.ShortTextMaxLength);
        IssuingOrganisation = TextValidation.RequireNonBlank(issuingOrganisation, nameof(issuingOrganisation), ValidationLimits.ShortTextMaxLength);
        IssuedAt = issuedAt;
        ExpiresAt = expiresAt;
        CredentialId = TextValidation.TrimToNullOrValidate(credentialId, nameof(credentialId), ValidationLimits.ShortTextMaxLength);
        Url = TextValidation.ValidateOptionalAbsoluteUrl(url, nameof(url), "http", "https");
    }
}
