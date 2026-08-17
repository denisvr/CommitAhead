using CommitAhead.Application.CVPresentations;

namespace CommitAhead.Infrastructure.Tests.CVPresentations;

/// <summary>
/// The one canonical sample <see cref="CVExportDocument"/>, shared by
/// <see cref="QuestPdfCVExportRendererTests"/> (parsed-text assertions) and
/// <see cref="QuestPdfCVExportRendererVisualRegressionTests"/> (pixel assertions against a
/// committed baseline). Both must render the exact same input for the visual baseline to mean
/// anything — a second, independently-maintained copy would drift silently.
/// </summary>
internal static class CVExportDocumentFixtures
{
    public static CVExportDocument Sample(int pageLimit = 3) => new(
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
}
