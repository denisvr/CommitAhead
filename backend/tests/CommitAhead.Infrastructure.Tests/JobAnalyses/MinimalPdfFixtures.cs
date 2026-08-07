using System.Security.Cryptography;
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
    public static byte[] MultiPage(int pageCount, string? pageText) =>
        BuildPdf(pageText is null ? new string?[pageCount] : Enumerable.Range(0, pageCount).Select(i => $"{pageText} {i}").ToArray());

    /// <summary>A valid multi-page PDF where page <c>i</c>'s content stream is exactly <paramref name="pageTexts"/>[i] — unlike <see cref="MultiPage"/>, nothing is appended, so a test can place an exact string at the very end/start of a page's text.</summary>
    public static byte[] MultiPageWithDistinctText(params string[] pageTexts) => BuildPdf(pageTexts);

    private static byte[] BuildPdf(string?[] pageTexts)
    {
        var pageCount = pageTexts.Length;
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
            var contentsRef = pageTexts[i] is null ? string.Empty : $"/Contents {fontId + 1 + i} 0 R";
            Write($"{3 + i} 0 obj<</Type/Page/Parent 2 0 R/MediaBox[0 0 612 792]/Resources<</Font<</F1 {fontId} 0 R>>>>{contentsRef}>>endobj\n");
        }

        offsets.Add(stream.Length);
        Write($"{fontId} 0 obj<</Type/Font/Subtype/Type1/BaseFont/Helvetica>>endobj\n");

        for (var i = 0; i < pageCount; i++)
        {
            var pageText = pageTexts[i];
            if (pageText is null)
            {
                continue;
            }

            var contentStream = $"BT /F1 24 Tf 100 700 Td ({pageText}) Tj ET";
            offsets.Add(stream.Length);
            Write($"{fontId + 1 + i} 0 obj<</Length {contentStream.Length}>>stream\n{contentStream}\nendstream endobj\n");
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

    /// <summary>
    /// A valid, structurally minimal PDF encrypted with the PDF standard security handler
    /// (Revision 2, 40-bit RC4 — ISO 32000-1 Algorithms 3.2/3.3/3.4), using a real, non-empty user
    /// password. PdfPig, opened without that password, only ever tries the empty user password by
    /// default, computes a non-matching U value, and fails authentication before reading any page
    /// content — so the page's own content stream is never actually RC4-encrypted; the point of
    /// this fixture is exercising the real /Encrypt-dictionary detection and authentication-failure
    /// path against the real PdfPig library, not full document encryption.
    /// </summary>
    public static byte[] Encrypted()
    {
        const string ownerPassword = "ownerSecret";
        const string userPassword = "userSecret";
        const int permissions = -1;
        var id = Encoding.ASCII.GetBytes("0123456789ABCDEF");

        var o = ComputeOwnerValue(ownerPassword, userPassword);
        var encryptionKey = ComputeEncryptionKey(userPassword, o, permissions, id);
        var u = Rc4(encryptionKey, Pad32);

        using var stream = new MemoryStream();
        void Write(string s)
        {
            var bytes = Encoding.ASCII.GetBytes(s);
            stream.Write(bytes, 0, bytes.Length);
        }

        static string Hex(byte[] bytes) => Convert.ToHexString(bytes);

        Write("%PDF-1.4\n");
        var offsets = new List<long>();

        offsets.Add(stream.Length);
        Write("1 0 obj<</Type/Catalog/Pages 2 0 R>>endobj\n");

        offsets.Add(stream.Length);
        Write("2 0 obj<</Type/Pages/Kids[3 0 R]/Count 1>>endobj\n");

        offsets.Add(stream.Length);
        Write("3 0 obj<</Type/Page/Parent 2 0 R/MediaBox[0 0 612 792]/Resources<</Font<</F1 4 0 R>>>>/Contents 5 0 R>>endobj\n");

        offsets.Add(stream.Length);
        Write("4 0 obj<</Type/Font/Subtype/Type1/BaseFont/Helvetica>>endobj\n");

        var contentStream = "BT /F1 24 Tf 100 700 Td (Should never be read) Tj ET";
        offsets.Add(stream.Length);
        Write($"5 0 obj<</Length {contentStream.Length}>>stream\n{contentStream}\nendstream endobj\n");

        offsets.Add(stream.Length);
        Write($"6 0 obj<</Filter/Standard/V 1/R 2/O<{Hex(o)}>/U<{Hex(u)}>/P {permissions}>>endobj\n");

        var totalObjects = offsets.Count + 1;
        var xrefOffset = stream.Length;
        Write($"xref\n0 {totalObjects}\n0000000000 65535 f \n");
        foreach (var offset in offsets)
        {
            Write($"{offset:D10} 00000 n \n");
        }

        Write($"trailer<</Size {totalObjects}/Root 1 0 R/Encrypt 6 0 R/ID[<{Hex(id)}>]>>\nstartxref\n{xrefOffset}\n%%EOF");
        return stream.ToArray();
    }

    // ISO 32000-1 Algorithm 3.2 step 1's fixed 32-byte padding string.
    private static readonly byte[] Pad32 =
    [
        0x28, 0xBF, 0x4E, 0x5E, 0x4E, 0x75, 0x8A, 0x41, 0x64, 0x00, 0x4E, 0x56, 0xFF, 0xFA, 0x01, 0x08,
        0x2E, 0x2E, 0x00, 0xB6, 0xD0, 0x68, 0x3E, 0x80, 0x2F, 0x0C, 0xA9, 0xFE, 0x64, 0x53, 0x69, 0x7A,
    ];

    private static byte[] PadPassword(string password)
    {
        var bytes = Encoding.Latin1.GetBytes(password);
        var padded = new byte[32];
        var copyLength = Math.Min(bytes.Length, 32);
        Array.Copy(bytes, padded, copyLength);
        Array.Copy(Pad32, 0, padded, copyLength, 32 - copyLength);
        return padded;
    }

    /// <summary>Algorithm 3.3 (computing the O value), Revision 2: no 50x re-hashing — that's Revision 3/4 only.</summary>
    private static byte[] ComputeOwnerValue(string ownerPassword, string userPassword)
    {
        var rc4Key = MD5.HashData(PadPassword(ownerPassword))[..5];
        return Rc4(rc4Key, PadPassword(userPassword));
    }

    /// <summary>Algorithm 3.2 (computing the encryption key), Revision 2: a single MD5 pass, first 5 bytes (40 bits).</summary>
    private static byte[] ComputeEncryptionKey(string userPassword, byte[] ownerValue, int permissions, byte[] id)
    {
        var permissionBytes = BitConverter.GetBytes(permissions);
        if (!BitConverter.IsLittleEndian)
        {
            Array.Reverse(permissionBytes);
        }

        using var md5 = IncrementalHash.CreateHash(HashAlgorithmName.MD5);
        md5.AppendData(PadPassword(userPassword));
        md5.AppendData(ownerValue);
        md5.AppendData(permissionBytes);
        md5.AppendData(id);
        return md5.GetHashAndReset()[..5];
    }

    private static byte[] Rc4(byte[] key, byte[] data)
    {
        var s = new byte[256];
        for (var i = 0; i < 256; i++)
        {
            s[i] = (byte)i;
        }

        var j = 0;
        for (var i = 0; i < 256; i++)
        {
            j = (j + s[i] + key[i % key.Length]) & 0xFF;
            (s[i], s[j]) = (s[j], s[i]);
        }

        var output = new byte[data.Length];
        int a = 0, b = 0;
        for (var k = 0; k < data.Length; k++)
        {
            a = (a + 1) & 0xFF;
            b = (b + s[a]) & 0xFF;
            (s[a], s[b]) = (s[b], s[a]);
            output[k] = (byte)(data[k] ^ s[(s[a] + s[b]) & 0xFF]);
        }

        return output;
    }
}
