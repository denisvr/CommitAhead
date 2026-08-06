using CommitAhead.Domain;

namespace CommitAhead.Domain.ProfessionalProfiles;

/// <summary>A canonical online presence link inside a ProfessionalProfile (CONTEXT.md).</summary>
public sealed class ProfileLink
{
    public Guid Id { get; }
    public ProfileLinkKind Kind { get; }
    public string? Label { get; }
    public string Url { get; }

    public ProfileLink(Guid id, ProfileLinkKind kind, string? label, string url)
    {
        if (id == Guid.Empty)
        {
            throw new DomainValidationException("Id is required.");
        }

        if (!Enum.IsDefined(kind))
        {
            throw new DomainValidationException("Kind is not a recognized value.");
        }

        Id = id;
        Kind = kind;
        Label = TextValidation.TrimToNullOrValidate(label, nameof(label), ValidationLimits.ShortTextMaxLength);
        Url = TextValidation.ValidateAbsoluteUrl(url, nameof(url), "http", "https");
    }
}
