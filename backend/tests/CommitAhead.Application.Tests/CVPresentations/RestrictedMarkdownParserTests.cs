using CommitAhead.Application.CVPresentations;

namespace CommitAhead.Application.Tests.CVPresentations;

public class RestrictedMarkdownParserTests
{
    [Fact]
    public void Parse_WithNullOrBlankInput_ReturnsNoBlocks()
    {
        Assert.Empty(RestrictedMarkdownParser.Parse(null));
        Assert.Empty(RestrictedMarkdownParser.Parse("   "));
    }

    [Fact]
    public void Parse_AParagraphWithBoldAndItalic_PreservesEmphasis()
    {
        var blocks = RestrictedMarkdownParser.Parse("Some **bold** and *italic* text.");

        var paragraph = Assert.IsType<MarkdownParagraph>(Assert.Single(blocks));
        var bold = Assert.Single(paragraph.Runs.OfType<MarkdownText>(), r => r.Bold);
        Assert.Equal("bold", bold.Text);
        var italic = Assert.Single(paragraph.Runs.OfType<MarkdownText>(), r => r.Italic);
        Assert.Equal("italic", italic.Text);
    }

    [Fact]
    public void Parse_AHeading_ReturnsAMarkdownHeadingWithItsLevel()
    {
        var blocks = RestrictedMarkdownParser.Parse("## Section");

        var heading = Assert.IsType<MarkdownHeading>(Assert.Single(blocks));
        Assert.Equal("Section", heading.Text);
        Assert.Equal(2, heading.Level);
    }

    [Fact]
    public void Parse_ABulletList_ReturnsOneItemPerLine()
    {
        var blocks = RestrictedMarkdownParser.Parse("- one\n- two\n- three");

        var list = Assert.IsType<MarkdownBulletList>(Assert.Single(blocks));
        Assert.Equal(3, list.Items.Count);
        Assert.Equal("one", Assert.IsType<MarkdownText>(list.Items[0].Single()).Text);
    }

    [Theory]
    [InlineData("https://example.com")]
    [InlineData("http://example.com")]
    [InlineData("mailto:someone@example.com")]
    public void Parse_ALinkWithAnAllowedScheme_KeepsItAsAHyperlink(string url)
    {
        var blocks = RestrictedMarkdownParser.Parse($"See [my site]({url}).");

        var paragraph = Assert.IsType<MarkdownParagraph>(Assert.Single(blocks));
        var link = Assert.Single(paragraph.Runs.OfType<MarkdownLink>());
        Assert.Equal("my site", link.Text);
        Assert.Equal(url, link.Url);
    }

    [Theory]
    [InlineData("javascript:alert(1)")]
    [InlineData("data:text/html,evil")]
    [InlineData("relative/path")]
    public void Parse_ALinkWithADisallowedOrRelativeUrl_KeepsOnlyItsTextNoHyperlink(string url)
    {
        var blocks = RestrictedMarkdownParser.Parse($"See [my site]({url}).");

        var paragraph = Assert.IsType<MarkdownParagraph>(Assert.Single(blocks));
        Assert.Empty(paragraph.Runs.OfType<MarkdownLink>());
        Assert.Contains(paragraph.Runs.OfType<MarkdownText>(), r => r.Text == "my site");
    }

    [Fact]
    public void Parse_AnImage_IsDroppedEntirely()
    {
        var blocks = RestrictedMarkdownParser.Parse("Before ![alt text](photo.png) after.");

        var paragraph = Assert.IsType<MarkdownParagraph>(Assert.Single(blocks));
        var text = string.Concat(paragraph.Runs.OfType<MarkdownText>().Select(r => r.Text));
        Assert.DoesNotContain("alt text", text);
        Assert.Contains("Before", text);
        Assert.Contains("after", text);
    }

    [Fact]
    public void Parse_RawHtml_StripsTheTagsButKeepsAnyPlainTextBetweenThem()
    {
        var blocks = RestrictedMarkdownParser.Parse("Before <script>alert(1)</script> after.");

        var paragraph = Assert.IsType<MarkdownParagraph>(Assert.Single(blocks));
        var text = string.Concat(paragraph.Runs.OfType<MarkdownText>().Select(r => r.Text));
        Assert.DoesNotContain("<script>", text);
        Assert.DoesNotContain("</script>", text);
        Assert.Contains("alert(1)", text);
    }
}
