namespace CommitAhead.Domain.StudyItems;

/// <summary>
/// Every length/count ceiling referenced by domain validation. <see cref="TitleMaxLength"/> and
/// <see cref="PriorityOverrideReasonMaxLength"/> are also referenced by the EF Core column
/// configuration for those two fields (Infrastructure), so the domain check and the database
/// column can never drift apart. The rest constrain JSONB-nested typed-details fields, which
/// have no EF-level column limit of their own — see docs/architecture/persistence.md.
/// </summary>
public static class ValidationLimits
{
    public const int TitleMaxLength = 200;
    public const int PriorityOverrideReasonMaxLength = 500;
    public const int TagMaxLength = 50;
    public const int MaxTagCount = 20;
    public const int ShortTextMaxLength = 200;
    public const int MarkdownMaxLength = 20_000;
    public const int ListEntryMaxLength = 500;
    public const int MaxListEntryCount = 50;
    public const int UrlMaxLength = 2000;
}
