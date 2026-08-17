using System.Runtime.CompilerServices;
using CommitAhead.Infrastructure.CVPresentations;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;

namespace CommitAhead.Infrastructure.Tests.CVPresentations;

/// <summary>
/// The deterministic, post-merge visual-regression fixture the roadmap deferred out of Phase 5's
/// own exit criteria — one committed baseline PNG per template, rasterised via
/// <see cref="QuestPdfCVExportRenderer.RenderPageImages"/> (the exact same document tree
/// <see cref="QuestPdfCVExportRenderer.Render"/> turns into the shipped PDF, not a
/// reimplementation). <see cref="QuestPdfCVExportRendererTests"/> already proves the *content* is
/// correct via PdfPig text extraction; this proves the *layout* — font sizing, spacing, borders,
/// column widths — hasn't silently shifted, which text extraction cannot detect at all.
///
/// Comparison is a tolerant per-pixel diff, not byte-for-byte PNG equality: Skia's own
/// anti-aliasing can differ by a pixel or two at glyph edges between runs/machines even when the
/// layout itself is identical, so an exact-bytes check would be a flaky test wearing a
/// deterministic-sounding name. The threshold below is intentionally tight — this is meant to
/// catch a real, visible regression, not merely to be reliably green.
/// </summary>
public class QuestPdfCVExportRendererVisualRegressionTests
{
    private const string TemplateKey = "modern-one-page";

    // A pixel counts as "different" only once its RGBA channels move enough to be a human-visible
    // change, not sub-pixel anti-aliasing noise.
    private const int ChannelDifferenceThreshold = 24;

    // Fraction of a page's pixels allowed to cross that threshold before the test fails. Loose
    // enough to absorb anti-aliasing jitter along text/border edges, tight enough that a shifted
    // section, resized font, or broken layout — which moves far more than a sliver of pixels —
    // still fails.
    private const double MaxDifferingPixelFraction = 0.01;

    [Fact]
    public void RenderPageImages_ForTheModernOnePageTemplate_MatchesTheCommittedVisualBaseline()
    {
        var renderer = new QuestPdfCVExportRenderer();
        var document = CVExportDocumentFixtures.Sample();

        var pages = renderer.RenderPageImages(document);
        Assert.Single(pages);

        var baselinePath = BaselinePath(TemplateKey, pageNumber: 1);
        Assert.True(
            File.Exists(baselinePath),
            $"No committed baseline at '{baselinePath}'. If the '{TemplateKey}' template changed " +
            "intentionally, run RegenerateBaseline_ModernOnePage (below, [Fact(Skip=...)]) once, " +
            "review the resulting image by eye, and commit it.");

        using var rendered = Image.Load<Rgba32>(pages[0]);
        using var baseline = Image.Load<Rgba32>(File.ReadAllBytes(baselinePath));

        Assert.Equal(baseline.Width, rendered.Width);
        Assert.Equal(baseline.Height, rendered.Height);

        var differingPixels = CountDifferingPixels(baseline, rendered);
        var totalPixels = baseline.Width * baseline.Height;
        var differingFraction = (double)differingPixels / totalPixels;

        Assert.True(
            differingFraction <= MaxDifferingPixelFraction,
            $"{differingPixels}/{totalPixels} pixels ({differingFraction:P2}) differ from the " +
            $"committed baseline at '{baselinePath}' — exceeds the {MaxDifferingPixelFraction:P2} " +
            "tolerance. If this is an intentional template change, regenerate the baseline (see " +
            "RegenerateBaseline_ModernOnePage below) and review the diff by eye before committing it.");
    }

    /// <summary>
    /// Not part of the regression suite — deliberately skipped so it never runs on CI or a normal
    /// local run. Run it explicitly, by name, only after an intentional change to the
    /// "modern-one-page" template, then look at the written PNG before committing it: this is the
    /// one place a wrong image becomes the new "correct" baseline forever, so nothing here writes
    /// silently as a side effect of an ordinary test run.
    /// </summary>
    [Fact(Skip = "Regenerates the committed visual baseline — run explicitly and review the output by eye before committing it.")]
    public void RegenerateBaseline_ModernOnePage()
    {
        var renderer = new QuestPdfCVExportRenderer();
        var document = CVExportDocumentFixtures.Sample();

        var pages = renderer.RenderPageImages(document);
        Assert.Single(pages);

        var baselinePath = BaselinePath(TemplateKey, pageNumber: 1);
        Directory.CreateDirectory(Path.GetDirectoryName(baselinePath)!);
        File.WriteAllBytes(baselinePath, pages[0]);
    }

    private static long CountDifferingPixels(Image<Rgba32> baseline, Image<Rgba32> rendered)
    {
        var differing = 0L;

        for (var y = 0; y < baseline.Height; y++)
        {
            for (var x = 0; x < baseline.Width; x++)
            {
                var a = baseline[x, y];
                var b = rendered[x, y];

                var delta = Math.Abs(a.R - b.R) + Math.Abs(a.G - b.G) + Math.Abs(a.B - b.B) + Math.Abs(a.A - b.A);
                if (delta > ChannelDifferenceThreshold)
                {
                    differing++;
                }
            }
        }

        return differing;
    }

    // [CallerFilePath] resolves to this .cs file's own path at compile time, so the baseline is
    // found next to the source under version control — never under bin/, and never dependent on
    // the test runner's working directory.
    private static string BaselinePath(string templateKey, int pageNumber, [CallerFilePath] string sourceFilePath = "") =>
        Path.Combine(Path.GetDirectoryName(sourceFilePath)!, "VisualBaselines", $"{templateKey}.page{pageNumber}.png");
}
