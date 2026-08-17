using CommitAhead.Application.CVPresentations;
using CommitAhead.Infrastructure.CVPresentations;
using UglyToad.PdfPig;

namespace CommitAhead.Infrastructure.Tests.CVPresentations;

/// <summary>
/// Parsed-output assertions against the real rendered PDF (via PdfPig, never a golden-file/visual
/// diff — that's <see cref="QuestPdfCVExportRendererVisualRegressionTests"/>) — proves the
/// template actually contains the data ExportCVPresentationUseCase resolved, not just that
/// QuestPDF didn't throw.
/// </summary>
public class QuestPdfCVExportRendererTests
{
    private static CVExportDocument CreateDocument(int pageLimit = 3) => CVExportDocumentFixtures.Sample(pageLimit);

    private static string ExtractText(byte[] pdfBytes)
    {
        using var document = PdfDocument.Open(pdfBytes);
        return string.Join("\n", document.GetPages().Select(p => p.Text));
    }

    [Fact]
    public void Render_ProducesAValidSinglePagePdf_ContainingEveryProvidedSection()
    {
        var renderer = new QuestPdfCVExportRenderer();

        var rendered = renderer.Render(CreateDocument());

        using var document = PdfDocument.Open(rendered.PdfBytes);
        Assert.Equal(1, document.NumberOfPages);
        Assert.Equal(1, rendered.PageCount);

        var text = ExtractText(rendered.PdfBytes);
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

        var text = ExtractText(renderer.Render(document).PdfBytes);

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

        var rendered = renderer.Render(document);

        using var pdf = PdfDocument.Open(rendered.PdfBytes);
        Assert.True(pdf.NumberOfPages > 1, "Expected the overflowing content to spill onto more than one page.");
        Assert.Equal(pdf.NumberOfPages, rendered.PageCount);
    }

    [Fact]
    public void Render_WithBulletListMarkdownInSummaryExperienceAndProjects_RendersEveryBullet()
    {
        IReadOnlyList<MarkdownBlock> bulletList(params string[] items) =>
            [new MarkdownBulletList(items.Select(item => (IReadOnlyList<MarkdownRun>)[new MarkdownText(item)]).ToList())];

        var document = CreateDocument() with
        {
            Summary = bulletList("Summary bullet one.", "Summary bullet two."),
            Experience =
            [
                new CVExportExperience(
                    "Acme Corp", null, "Senior Engineer", "Permanent", "Remote", "Remote", "Jan 2020 – Jun 2023",
                    bulletList("Experience bullet one.", "Experience bullet two."), []),
            ],
            Projects =
            [
                new CVExportProject("Side Project", null, "2021", bulletList("Project bullet one.", "Project bullet two."), null),
            ],
        };
        var renderer = new QuestPdfCVExportRenderer();

        var rendered = renderer.Render(document);

        var text = ExtractText(rendered.PdfBytes);
        Assert.Contains("Summary bullet one.", text);
        Assert.Contains("Summary bullet two.", text);
        Assert.Contains("Experience bullet one.", text);
        Assert.Contains("Experience bullet two.", text);
        Assert.Contains("Project bullet one.", text);
        Assert.Contains("Project bullet two.", text);
    }

    [Fact]
    public void Render_WithClientLanguageCertificationExpiryCredentialAndUrlFields_RendersEveryOne()
    {
        var document = CreateDocument() with
        {
            Experience =
            [
                new CVExportExperience(
                    "Acme Corp", "Big Client Inc", "Senior Engineer", "Permanent", "Remote", "Remote", "Jan 2020 – Jun 2023",
                    [new MarkdownParagraph([new MarkdownText("Led backend systems.")])], []),
            ],
            Languages = [new CVExportLanguage("French", "Fluent", "DELF B2")],
            Certifications =
            [
                new CVExportCertification(
                    "AWS Certified Developer", "Amazon", "Jan 2022", "Jan 2025", "CRED-12345", "https://aws.example.com/verify"),
            ],
            Projects =
            [
                new CVExportProject(
                    "Side Project", null, "2021", [new MarkdownParagraph([new MarkdownText("A hobby project.")])], "https://github.com/example/side-project"),
            ],
        };
        var renderer = new QuestPdfCVExportRenderer();

        var text = ExtractText(renderer.Render(document).PdfBytes);

        Assert.Contains("Big Client Inc", text);
        Assert.Contains("DELF B2", text);
        Assert.Contains("Jan 2025", text);
        Assert.Contains("CRED-12345", text);
        Assert.Contains("aws.example.com/verify", text);
        Assert.Contains("github.com/example/side-project", text);
    }
}
