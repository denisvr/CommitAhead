using Markdig.Syntax;
using Markdig.Syntax.Inlines;

namespace CommitAhead.Application.CVPresentations;

/// <summary>
/// Parses a Markdown string into a sanitised <see cref="MarkdownBlock"/> tree for CV export
/// rendering, applying the exact same allowlist the frontend's RestrictedMarkdown/
/// restrictedUrlTransform already enforce (threat-model.md's "same pipeline, no exceptions"):
/// no images, no raw HTML, links kept only for the https/http/mailto schemes — everything else is
/// stripped down to its own visible text with no link. Markdig itself never executes anything; this
/// walk exists purely to drop node kinds the allowlist forbids before the renderer ever sees them.
/// </summary>
internal static class RestrictedMarkdownParser
{
    private static readonly string[] AllowedSchemes = ["http", "https", "mailto"];

    public static IReadOnlyList<MarkdownBlock> Parse(string? markdown)
    {
        if (string.IsNullOrWhiteSpace(markdown))
        {
            return [];
        }

        var document = Markdig.Markdown.Parse(markdown);
        var blocks = new List<MarkdownBlock>();
        foreach (var block in document)
        {
            AppendBlock(blocks, block);
        }

        return blocks;
    }

    private static void AppendBlock(List<MarkdownBlock> blocks, Block block)
    {
        switch (block)
        {
            case HeadingBlock heading:
                blocks.Add(new MarkdownHeading(ExtractPlainText(heading.Inline), heading.Level));
                break;
            case ParagraphBlock paragraph:
                blocks.Add(new MarkdownParagraph(ExtractRuns(paragraph.Inline)));
                break;
            case ListBlock list:
                var items = list.OfType<ListItemBlock>()
                    .Select(item => item.OfType<ParagraphBlock>().SelectMany(p => ExtractRuns(p.Inline)).ToList())
                    .Where(runs => runs.Count > 0)
                    .ToList();
                if (items.Count > 0)
                {
                    blocks.Add(new MarkdownBulletList(items));
                }

                break;
            case QuoteBlock quote:
                foreach (var sub in quote.OfType<ParagraphBlock>())
                {
                    blocks.Add(new MarkdownParagraph(ExtractRuns(sub.Inline)));
                }

                break;
            case CodeBlock code:
                // Fenced/indented code has no Markdown inline syntax of its own — rendered as
                // plain text, never as raw HTML (there is nothing HTML-shaped to strip here).
                var text = code.Lines.ToString();
                if (!string.IsNullOrWhiteSpace(text))
                {
                    blocks.Add(new MarkdownParagraph([new MarkdownText(text)]));
                }

                break;

            // ThematicBreakBlock, HtmlBlock, and any other block kind are intentionally dropped —
            // an HtmlBlock in particular is exactly the raw-HTML case the allowlist forbids.
        }
    }

    private static string ExtractPlainText(ContainerInline? container)
    {
        if (container is null)
        {
            return string.Empty;
        }

        return string.Concat(ExtractRuns(container).Select(run => run switch
        {
            MarkdownText text => text.Text,
            MarkdownLink link => link.Text,
            _ => string.Empty,
        }));
    }

    private static List<MarkdownRun> ExtractRuns(ContainerInline? container)
    {
        var runs = new List<MarkdownRun>();
        if (container is null)
        {
            return runs;
        }

        AppendRuns(runs, container, bold: false, italic: false);
        return runs;
    }

    private static void AppendRuns(List<MarkdownRun> runs, ContainerInline container, bool bold, bool italic)
    {
        foreach (var inline in container)
        {
            switch (inline)
            {
                case LiteralInline literal:
                    runs.Add(new MarkdownText(literal.Content.ToString(), bold, italic));
                    break;
                case CodeInline code:
                    runs.Add(new MarkdownText(code.Content, bold, italic));
                    break;
                case LineBreakInline:
                    runs.Add(new MarkdownText(" ", bold, italic));
                    break;
                case EmphasisInline emphasis:
                    // DelimiterCount 2 is CommonMark strong emphasis (**/__); 1 is plain emphasis
                    // (*/_) — nesting (e.g. bold-italic) combines rather than overrides.
                    AppendRuns(runs, emphasis, bold || emphasis.DelimiterCount >= 2, italic || emphasis.DelimiterCount == 1);
                    break;
                case LinkInline { IsImage: true }:
                    // No images ever (RestrictedMarkdown.tsx: `img` -> null) — the alt text carries
                    // no information a reader needs and could itself be attacker-controlled.
                    break;
                case LinkInline link:
                    AppendLink(runs, link, bold, italic);
                    break;
                case HtmlInline:
                    // Raw HTML tags are dropped outright; any literal text between them survives
                    // as its own separate LiteralInline and is kept as inert plain text.
                    break;
                case ContainerInline nestedContainer:
                    AppendRuns(runs, nestedContainer, bold, italic);
                    break;
            }
        }
    }

    private static void AppendLink(List<MarkdownRun> runs, LinkInline link, bool bold, bool italic)
    {
        var text = ExtractPlainText(link);
        if (string.IsNullOrEmpty(text))
        {
            return;
        }

        if (IsAllowedLinkUrl(link.Url))
        {
            runs.Add(new MarkdownLink(text, link.Url!));
        }
        else
        {
            // Matches restrictedUrlTransform.ts returning '' for a disallowed scheme: the link's
            // own visible text survives, just with no href.
            runs.Add(new MarkdownText(text, bold, italic));
        }
    }

    /// <summary>
    /// Mirrors restrictedUrlTransform.ts exactly: finds the scheme separator (the first colon that
    /// precedes any of '/', '?', '#') and allow-lists only http/https/mailto. A relative or
    /// scheme-less URL has no separator and is treated as not allowed here — unlike the frontend
    /// (which can resolve a relative URL against the page), a PDF has no base URL to resolve
    /// against, so a "safe" relative link would just be a dead, non-clickable string; rendering it
    /// as plain text instead loses nothing the reader could have used.
    /// </summary>
    private static bool IsAllowedLinkUrl(string? url)
    {
        if (string.IsNullOrEmpty(url))
        {
            return false;
        }

        var colonIndex = url.IndexOf(':');
        if (colonIndex <= 0)
        {
            return false;
        }

        var firstSeparator = url.IndexOfAny(['/', '?', '#']);
        if (firstSeparator != -1 && firstSeparator < colonIndex)
        {
            return false;
        }

        var scheme = url[..colonIndex];
        return AllowedSchemes.Contains(scheme, StringComparer.OrdinalIgnoreCase);
    }
}
