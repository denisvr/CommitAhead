namespace CommitAhead.Domain.StudyItems;

/// <summary>
/// Discriminated union matching StudyItem.Category (ADR-0001). Plain C# types with no
/// serialization concerns — JSON (de)serialization is an Infrastructure-only responsibility
/// (see docs/architecture/persistence.md, "Typed category details").
/// </summary>
public abstract class StudyItemDetails
{
}
