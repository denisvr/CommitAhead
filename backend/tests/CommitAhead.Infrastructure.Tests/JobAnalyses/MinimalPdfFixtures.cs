using System.Text;

namespace CommitAhead.Infrastructure.Tests.JobAnalyses;

/// <summary>
/// Hand-crafted minimal valid PDF byte sequences, built directly from the PDF spec's own minimal
/// object/xref/trailer structure — never authored with PdfPig itself (this app's Infrastructure
/// adapter only ever reads PDFs, never creates them; that boundary holds in tests too).
/// </summary>
internal static class MinimalPdfFixtures
{
    /// <summary>A single-page valid PDF whose one content stream renders <paramref name="pageText"/>.</summary>
    public static byte[] SinglePage(string pageText) => MultiPage(1, pageText);

    /// <summary>A valid PDF with <paramref name="pageCount"/> pages. When <paramref name="pageText"/> is null, every page has no Contents entry at all (image-only simulation — no text to extract). Otherwise every page repeats "{pageText} {index}" once.</summary>
    public static byte[] MultiPage(int pageCount, string? pageText)
    {
        using var stream = new MemoryStream();
        void Write(string s)
        {
            var bytes = Encoding.ASCII.GetBytes(s);
            stream.Write(bytes, 0, bytes.Length);
        }

        Write("%PDF-1.4\n");
        var offsets = new List<long>();

        offsets.Add(stream.Length);
        Write("1 0 obj<</Type/Catalog/Pages 2 0 R>>endobj\n");

        var kids = string.Join(" ", Enumerable.Range(0, pageCount).Select(i => $"{3 + i} 0 R"));
        offsets.Add(stream.Length);
        Write($"2 0 obj<</Type/Pages/Kids[{kids}]/Count {pageCount}>>endobj\n");

        var fontId = 3 + pageCount;
        for (var i = 0; i < pageCount; i++)
        {
            offsets.Add(stream.Length);
            var contentsRef = pageText is null ? string.Empty : $"/Contents {fontId + 1 + i} 0 R";
            Write($"{3 + i} 0 obj<</Type/Page/Parent 2 0 R/MediaBox[0 0 612 792]/Resources<</Font<</F1 {fontId} 0 R>>>>{contentsRef}>>endobj\n");
        }

        offsets.Add(stream.Length);
        Write($"{fontId} 0 obj<</Type/Font/Subtype/Type1/BaseFont/Helvetica>>endobj\n");

        if (pageText is not null)
        {
            for (var i = 0; i < pageCount; i++)
            {
                var contentStream = $"BT /F1 24 Tf 100 700 Td ({pageText} {i}) Tj ET";
                offsets.Add(stream.Length);
                Write($"{fontId + 1 + i} 0 obj<</Length {contentStream.Length}>>stream\n{contentStream}\nendstream endobj\n");
            }
        }

        var totalObjects = offsets.Count + 1;
        var xrefOffset = stream.Length;
        Write($"xref\n0 {totalObjects}\n0000000000 65535 f \n");
        foreach (var offset in offsets)
        {
            Write($"{offset:D10} 00000 n \n");
        }

        Write($"trailer<</Size {totalObjects}/Root 1 0 R>>\nstartxref\n{xrefOffset}\n%%EOF");
        return stream.ToArray();
    }

    public static readonly byte[] Malformed = Encoding.ASCII.GetBytes("This is not a PDF at all.");
}
