using System.Text.Json;
using CommitAhead.Domain.ProfessionalProfiles;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace CommitAhead.Infrastructure.ProfessionalProfiles;

/// <summary>
/// Same reason as YearMonthConversion: ContactInfo is constructor-only, so ProfessionalProfile's
/// own constructor parameter can't be bound to it as a nested owned/complex type. Serialized whole
/// into a single jsonb column instead — mirrors StudyItemDetailsValueConverter's pattern, minus a
/// custom JsonConverter since ContactInfo isn't polymorphic; System.Text.Json's default constructor
/// matching (parameter names to property names, case-insensitive) round-trips it correctly.
/// </summary>
internal sealed class ContactInfoValueConverter : ValueConverter<ContactInfo, string>
{
    public ContactInfoValueConverter()
        : base(
            contactInfo => JsonSerializer.Serialize(contactInfo),
            json => JsonSerializer.Deserialize<ContactInfo>(json)!)
    {
    }
}
