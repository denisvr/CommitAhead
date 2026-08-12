namespace CommitAhead.Application.CVPresentations;

/// <summary>
/// A sanitised, structured representation of one Markdown field, produced by
/// <see cref="RestrictedMarkdownParser"/> — the CV export renderer walks this instead of the raw
/// Markdown string, so no HTML/script/image content or disallowed link scheme can ever reach it
/// (threat-model.md: "CV/PDF export: same allowlist and sanitisation before HTML generation").
/// </summary>
public abstract record MarkdownBlock;

public sealed record MarkdownHeading(string Text, int Level) : MarkdownBlock;

public sealed record MarkdownParagraph(IReadOnlyList<MarkdownRun> Runs) : MarkdownBlock;

public sealed record MarkdownBulletList(IReadOnlyList<IReadOnlyList<MarkdownRun>> Items) : MarkdownBlock;

public abstract record MarkdownRun;

public sealed record MarkdownText(string Text, bool Bold = false, bool Italic = false) : MarkdownRun;

/// <summary>Only ever constructed for an https/http/mailto URL — see RestrictedMarkdownParser.IsAllowedLinkUrl.</summary>
public sealed record MarkdownLink(string Text, string Url) : MarkdownRun;
