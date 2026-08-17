using CommitAhead.Application.CVPresentations;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using UglyToad.PdfPig;

namespace CommitAhead.Infrastructure.CVPresentations;

/// <summary>
/// The one export template ADR-0020/Phase 5 requires — a single-column layout covering every
/// section a CVExportDocument can carry. Pure layout: every business decision (what's included,
/// how dates read, what Markdown survives) was already made by ExportCVPresentationUseCase.
/// </summary>
public sealed class QuestPdfCVExportRenderer : IExportRenderer
{
    static QuestPdfCVExportRenderer()
    {
        // Community License — see ADR-0020 for its actual eligibility terms and when to
        // reassess them. Must be set once before any Document.Create call, so a static
        // constructor (run exactly once per process, before the first Render) is the natural
        // place, not a per-call assignment.
        QuestPDF.Settings.License = LicenseType.Community;
    }

    public RenderedCVExport Render(CVExportDocument document)
    {
        var pdfBytes = BuildDocument(document).GeneratePdf();

        int pageCount;
        using (var opened = PdfDocument.Open(pdfBytes))
        {
            pageCount = opened.NumberOfPages;
        }

        return new RenderedCVExport(pdfBytes, pageCount);
    }

    /// <summary>
    /// One PNG per page, rasterised from the exact same document tree <see cref="Render"/> turns
    /// into PDF bytes — never a separately-maintained rendering path — so a visual-regression
    /// fixture comparing these images is actually exercising production layout code, not a
    /// reimplementation of it. Not part of <see cref="IExportRenderer"/>: no production caller
    /// needs a raster image, only the visual-regression test fixture does.
    /// </summary>
    public IReadOnlyList<byte[]> RenderPageImages(CVExportDocument document)
    {
        var settings = new ImageGenerationSettings
        {
            ImageFormat = ImageFormat.Png,
            RasterDpi = 144,
        };

        return BuildDocument(document).GenerateImages(settings).ToList();
    }

    private static IDocument BuildDocument(CVExportDocument document)
    {
        return Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(32, Unit.Point);
                page.DefaultTextStyle(x => x.FontSize(10));

                page.Header().Element(c => RenderHeader(c, document));
                page.Content().PaddingTop(8).Column(column =>
                {
                    column.Spacing(10);

                    if (document.Summary.Count > 0)
                    {
                        column.Item().Element(c => RenderMarkdownBlocks(c, document.Summary));
                    }

                    if (document.Experience.Count > 0)
                    {
                        column.Item().Element(c => RenderExperienceSection(c, document.Experience));
                    }

                    if (document.Education.Count > 0)
                    {
                        column.Item().Element(c => RenderEducationSection(c, document.Education));
                    }

                    if (document.Skills.Count > 0)
                    {
                        column.Item().Element(c => RenderSkillsSection(c, document.Skills));
                    }

                    if (document.Languages.Count > 0)
                    {
                        column.Item().Element(c => RenderLanguagesSection(c, document.Languages));
                    }

                    if (document.Certifications.Count > 0)
                    {
                        column.Item().Element(c => RenderCertificationsSection(c, document.Certifications));
                    }

                    if (document.Projects.Count > 0)
                    {
                        column.Item().Element(c => RenderProjectsSection(c, document.Projects));
                    }

                    if (document.ProfileLinks.Count > 0)
                    {
                        column.Item().Element(c => RenderProfileLinksSection(c, document.ProfileLinks));
                    }
                });

                page.Footer().AlignCenter().Text(text =>
                {
                    text.CurrentPageNumber();
                    text.Span(" / ");
                    text.TotalPages();
                });
            });
        });
    }

    private static void RenderHeader(IContainer container, CVExportDocument document)
    {
        container.Column(column =>
        {
            column.Item().Text(document.Contact.Name).FontSize(20).Bold();

            var subtitle = document.TargetRole is null ? document.TargetMarket : $"{document.TargetRole} — {document.TargetMarket}";
            column.Item().Text(subtitle).FontSize(12).FontColor(Colors.Grey.Darken2);

            var contactParts = new List<string>();
            if (document.Contact.Email is { Length: > 0 } email)
            {
                contactParts.Add(email);
            }

            if (document.Contact.Phone is { Length: > 0 } phone)
            {
                contactParts.Add(phone);
            }

            if (document.Contact.Address is { Length: > 0 } address)
            {
                contactParts.Add(address);
            }

            if (contactParts.Count > 0)
            {
                column.Item().Text(string.Join("  ·  ", contactParts)).FontSize(9).FontColor(Colors.Grey.Darken1);
            }
        });
    }

    private static void RenderSectionHeading(ColumnDescriptor column, string title)
    {
        column.Item().PaddingBottom(2).BorderBottom(0.75f, Unit.Point).BorderColor(Colors.Grey.Darken1)
            .Text(title.ToUpperInvariant()).FontSize(11).Bold().FontColor(Colors.Grey.Darken3);
    }

    private static void RenderExperienceSection(IContainer container, IReadOnlyList<CVExportExperience> entries)
    {
        container.Column(column =>
        {
            column.Spacing(6);
            RenderSectionHeading(column, "Experience");

            foreach (var entry in entries)
            {
                column.Item().Column(entryColumn =>
                {
                    entryColumn.Item().Row(row =>
                    {
                        row.RelativeItem().Text($"{entry.Role} — {entry.Company}").FontSize(10).Bold();
                        row.ConstantItem(120).AlignRight().Text(entry.DateRange).FontSize(9).FontColor(Colors.Grey.Darken1);
                    });

                    var meta = string.Join(" · ", new[] { entry.EmploymentType, entry.WorkMode, entry.Location }.Where(v => !string.IsNullOrWhiteSpace(v)));
                    if (meta.Length > 0)
                    {
                        entryColumn.Item().Text(meta).FontSize(8).Italic().FontColor(Colors.Grey.Darken1);
                    }

                    if (entry.Client is { Length: > 0 } client)
                    {
                        entryColumn.Item().Text($"Client: {client}").FontSize(8).Italic().FontColor(Colors.Grey.Darken1);
                    }

                    if (entry.Summary.Count > 0)
                    {
                        entryColumn.Item().Element(c => RenderMarkdownBlocks(c, entry.Summary));
                    }

                    if (entry.Achievements.Count > 0)
                    {
                        entryColumn.Item().Element(c => RenderBulletList(c, entry.Achievements.Select(a => (IReadOnlyList<MarkdownRun>)[new MarkdownText(a)]).ToList()));
                    }
                });
            }
        });
    }

    private static void RenderEducationSection(IContainer container, IReadOnlyList<CVExportEducation> entries)
    {
        container.Column(column =>
        {
            column.Spacing(6);
            RenderSectionHeading(column, "Education");

            foreach (var entry in entries)
            {
                column.Item().Column(entryColumn =>
                {
                    entryColumn.Item().Row(row =>
                    {
                        var title = entry.Field is null ? entry.Degree : $"{entry.Degree}, {entry.Field}";
                        row.RelativeItem().Text($"{title} — {entry.Institution}").FontSize(10).Bold();
                        row.ConstantItem(120).AlignRight().Text(entry.DateRange).FontSize(9).FontColor(Colors.Grey.Darken1);
                    });

                    if (!string.IsNullOrWhiteSpace(entry.Location))
                    {
                        entryColumn.Item().Text(entry.Location).FontSize(8).Italic().FontColor(Colors.Grey.Darken1);
                    }

                    if (entry.Details.Count > 0)
                    {
                        entryColumn.Item().Element(c => RenderMarkdownBlocks(c, entry.Details));
                    }
                });
            }
        });
    }

    private static void RenderSkillsSection(IContainer container, IReadOnlyList<string> skills)
    {
        container.Column(column =>
        {
            RenderSectionHeading(column, "Skills");
            column.Item().Text(string.Join("  ·  ", skills)).FontSize(9);
        });
    }

    private static void RenderLanguagesSection(IContainer container, IReadOnlyList<CVExportLanguage> languages)
    {
        container.Column(column =>
        {
            RenderSectionHeading(column, "Languages");
            var text = string.Join("  ·  ", languages.Select(l => l.Certification is { Length: > 0 } certification
                ? $"{l.Language} ({l.Proficiency}, {certification})"
                : $"{l.Language} ({l.Proficiency})"));
            column.Item().Text(text).FontSize(9);
        });
    }

    private static void RenderCertificationsSection(IContainer container, IReadOnlyList<CVExportCertification> certifications)
    {
        container.Column(column =>
        {
            column.Spacing(4);
            RenderSectionHeading(column, "Certifications");

            foreach (var certification in certifications)
            {
                column.Item().Column(entryColumn =>
                {
                    entryColumn.Item().Row(row =>
                    {
                        row.RelativeItem().Text($"{certification.Name} — {certification.IssuingOrganisation}").FontSize(9.5f);

                        var dateRange = (certification.IssuedAt, certification.ExpiresAt) switch
                        {
                            (null, null) => null,
                            (var issued, null) => issued,
                            (null, var expires) => $"– {expires}",
                            (var issued, var expires) => $"{issued} – {expires}",
                        };

                        if (dateRange is { Length: > 0 })
                        {
                            row.ConstantItem(100).AlignRight().Text(dateRange).FontSize(9).FontColor(Colors.Grey.Darken1);
                        }
                    });

                    if (certification.CredentialId is { Length: > 0 } credentialId)
                    {
                        entryColumn.Item().Text($"Credential ID: {credentialId}").FontSize(8).Italic().FontColor(Colors.Grey.Darken1);
                    }

                    if (certification.Url is { Length: > 0 } url)
                    {
                        entryColumn.Item().Text(t => t.Hyperlink(url, url).FontSize(8.5f).Underline());
                    }
                });
            }
        });
    }

    private static void RenderProjectsSection(IContainer container, IReadOnlyList<CVExportProject> projects)
    {
        container.Column(column =>
        {
            column.Spacing(6);
            RenderSectionHeading(column, "Projects");

            foreach (var project in projects)
            {
                column.Item().Column(entryColumn =>
                {
                    entryColumn.Item().Row(row =>
                    {
                        var title = project.Role is null ? project.Name : $"{project.Name} — {project.Role}";
                        row.RelativeItem().Text(title).FontSize(10).Bold();
                        row.ConstantItem(120).AlignRight().Text(project.DateRange).FontSize(9).FontColor(Colors.Grey.Darken1);
                    });

                    if (project.Description.Count > 0)
                    {
                        entryColumn.Item().Element(c => RenderMarkdownBlocks(c, project.Description));
                    }

                    if (project.Url is { Length: > 0 } url)
                    {
                        entryColumn.Item().Text(t => t.Hyperlink(url, url).FontSize(8.5f).Underline());
                    }
                });
            }
        });
    }

    private static void RenderProfileLinksSection(IContainer container, IReadOnlyList<CVExportLink> links)
    {
        container.Column(column =>
        {
            RenderSectionHeading(column, "Links");
            column.Item().Text(text =>
            {
                for (var i = 0; i < links.Count; i++)
                {
                    if (i > 0)
                    {
                        text.Span("   ");
                    }

                    text.Hyperlink(links[i].Label, links[i].Url).FontSize(9).Underline();
                }
            });
        });
    }

    private static void RenderMarkdownBlocks(IContainer container, IReadOnlyList<MarkdownBlock> blocks)
    {
        container.Column(column =>
        {
            column.Spacing(3);
            foreach (var block in blocks)
            {
                switch (block)
                {
                    case MarkdownHeading heading:
                        column.Item().Text(heading.Text).FontSize(10 + Math.Max(0, 3 - heading.Level)).Bold();
                        break;
                    case MarkdownParagraph paragraph:
                        column.Item().Element(c => RenderRuns(c, paragraph.Runs));
                        break;
                    case MarkdownBulletList bulletList:
                        column.Item().Element(c => RenderBulletList(c, bulletList.Items));
                        break;
                }
            }
        });
    }

    private static void RenderBulletList(IContainer container, IReadOnlyList<IReadOnlyList<MarkdownRun>> items)
    {
        container.Column(column =>
        {
            column.Spacing(1);
            foreach (var item in items)
            {
                column.Item().Row(row =>
                {
                    row.ConstantItem(10).Text("•");
                    row.RelativeItem().Element(c => RenderRuns(c, item));
                });
            }
        });
    }

    private static void RenderRuns(IContainer container, IReadOnlyList<MarkdownRun> runs)
    {
        container.Text(text =>
        {
            foreach (var run in runs)
            {
                switch (run)
                {
                    case MarkdownLink link:
                        text.Hyperlink(link.Text, link.Url).Underline();
                        break;
                    case MarkdownText markdownText:
                        var span = text.Span(markdownText.Text);
                        if (markdownText.Bold)
                        {
                            span = span.Bold();
                        }

                        if (markdownText.Italic)
                        {
                            span = span.Italic();
                        }

                        break;
                }
            }
        });
    }
}
