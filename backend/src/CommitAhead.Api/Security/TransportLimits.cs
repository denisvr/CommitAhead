namespace CommitAhead.Api.Security;

/// <summary>
/// Finite HTTP-level ceilings. The domain already bounds text lengths and collection counts
/// (<c>ValidationLimits</c>), but those only apply after a body has been fully read and parsed —
/// these bound the work before that point.
/// </summary>
public static class TransportLimits
{
    /// <summary>
    /// Derived from the largest domain-valid payload, not guessed, so it can never reject input the
    /// domain would accept. The worst case is a Replace* collection endpoint at every ceiling
    /// simultaneously: 50 entries (MaxListEntryCount), each carrying a 20,000-character
    /// SummaryMarkdown (MarkdownMaxLength), 50 achievements of 500 characters
    /// (MaxListEntryCount x ListEntryMaxLength), a handful of 200-character fields, and 50 GUID
    /// skill references — roughly 48 KB per entry, so about 2.4 MB, plus JSON structure. 4 MiB
    /// leaves headroom while staying 7.5x below Kestrel's 30 MB default.
    /// </summary>
    public const long MaxRequestBodyBytes = 4L * 1024 * 1024;

    /// <summary>
    /// Real request and response graphs nest about five levels deep. 32 is far above anything the
    /// contract produces and far below the depth needed to make the parser itself the attack.
    /// </summary>
    public const int MaxJsonDepth = 32;

    /// <summary>
    /// Every state-changing request is counted, per caller, per minute. The profile editor persists
    /// on every add, edit, delete, and reorder, so this has to sit well above human interaction
    /// speed; it exists to bound automated abuse, not to pace a user.
    /// </summary>
    public const int WritesPerMinute = 120;

    /// <summary>
    /// CV export renders a PDF in process, which is the most expensive thing an authenticated caller
    /// can ask for, so it is bounded far more tightly than an ordinary write.
    /// </summary>
    public const int ExportsPerWindow = 10;

    public static readonly TimeSpan ExportWindow = TimeSpan.FromMinutes(5);

    public static readonly TimeSpan WriteWindow = TimeSpan.FromMinutes(1);
}
