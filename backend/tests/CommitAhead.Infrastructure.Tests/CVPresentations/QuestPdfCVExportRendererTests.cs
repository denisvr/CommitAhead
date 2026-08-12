using CommitAhead.Application.CVPresentations;
using CommitAhead.Infrastructure.CVPresentations;
using UglyToad.PdfPig;

namespace CommitAhead.Infrastructure.Tests.CVPresentations;

/// <summary>
/// Parsed-output assertions against the real rendered PDF (via PdfPig, never a golden-file/visual
/// diff — those are the separate, post-merge-only visual-regression fixtures the roadmap already
/// defers) — proves the template actually contains the data ExportCVPresentationUseCase resolved,
/// not just that QuestPDF didn't throw.
/// </summary>
public class QuestPdfCVExportRendererTests
{
    private static CVExportDocument CreateDocument(int pageLimit = 3) => new(
        "US Resume",
        "United States",
        "Backend Engineer",
        pageLimit,
        new CVExportContact("Ada Lovelace", "ada@example.com", "+1 555 0100", "123 Analytical Engine St"),
        [new MarkdownParagraph([new MarkdownText("A concise professional summary.")])],
        [
            new CVExportExperience(
                "Acme Corp", null, "Senior Engineer", "Permanent", "Remote", "Remote",
                "Jan 2020 – Jun 2023",
                [new MarkdownParagraph([new MarkdownText("Led backend systems.")])],
                ["Shipped the payments platform."]),
        ],
        [new CVExportEducation("State University", "BSc Computer Science", null, null, "Sep 2016 – Jun 2020", [])],
        ["C#", "PostgreSQL"],
        [new CVExportLanguage("English", "Native", null)],
        [new CVExportCertification("AWS Certified Developer", "Amazon", "Jan 2022", null, null, null)],
        [new CVExportProject("Side Project", null, "2021", [new MarkdownParagraph([new MarkdownText("A hobby project.")])], null)],
        [new CVExportLink("GitHub", "https://github.com/example")]);

    private static string ExtractText(byte[] pdfBytes)
    {
        using var document = PdfDocument.Open(pdfBytes);
        return string.Join("\n", document.GetPages().Select(p => p.Text));
    }

    [Fact]
    public void Render_ProducesAValidSinglePagePdf_ContainingEveryProvidedSection()
    {
        var renderer = new QuestPdfCVExportRenderer();

        var bytes = renderer.Render(CreateDocument());

        using var document = PdfDocument.Open(bytes);
        Assert.Equal(1, document.NumberOfPages);

        var text = ExtractText(bytes);
        Assert.Contains("Ada Lovelace", text);
        Assert.Contains("ada@example.com", text);
        Assert.Contains("+1 555 0100", text);
        Assert.Contains("Acme Corp", text);
        Assert.Contains("Senior Engineer", text);
        Assert.Contains("Shipped the payments platform.", text);
        Assert.Contains("State University", text);
        Assert.Contains("C#", text);
        Assert.Contains("PostgreSQL", text);
        Assert.Contains("English", text);
        Assert.Contains("AWS Certified Developer", text);
        Assert.Contains("Side Project", text);
        Assert.Contains("GitHub", text);
    }

    [Fact]
    public void Render_WhenAContactFieldIsExcluded_OmitsItFromTheOutput()
    {
        var document = CreateDocument() with { Contact = new CVExportContact("Ada Lovelace", null, null, null) };
        var renderer = new QuestPdfCVExportRenderer();

        var text = ExtractText(renderer.Render(document));

        Assert.Contains("Ada Lovelace", text);
        Assert.DoesNotContain("ada@example.com", text);
        Assert.DoesNotContain("+1 555 0100", text);
    }

    [Fact]
    public void Render_WithEnoughContentToOverflowOnePage_ProducesMorePages()
    {
        var manyExperiences = Enumerable.Range(1, 30)
            .Select(i => new CVExportExperience(
                $"Company {i}", null, $"Role {i}", "Permanent", "Remote", "Remote", "2020 – 2021",
                [new MarkdownParagraph([new MarkdownText("A long enough summary to take up real vertical space on the page.")])],
                ["Achievement one.", "Achievement two.", "Achievement three."]))
            .ToList();

        var document = CreateDocument(pageLimit: 10) with { Experience = manyExperiences };
        var renderer = new QuestPdfCVExportRenderer();

        var bytes = renderer.Render(document);

        using var pdf = PdfDocument.Open(bytes);
        Assert.True(pdf.NumberOfPages > 1, "Expected the overflowing content to spill onto more than one page.");
    }
}
