// Browser Picker Pro — Created by Parlee Conseiller, Inc.
using BrowserPickerPro.Core.Models;
using FluentAssertions;
using Xunit;

namespace BrowserPickerPro.UnitTests.BrowserDetection;

public class UrlRequestTests
{
    [Theory]
    [InlineData("https://google.com", "google.com", true, false)]
    [InlineData("http://example.com/path?q=1", "example.com", true, false)]
    [InlineData("ftp://files.server.com", "files.server.com", true, false)]
    public void Parse_HttpUrls(string input, string expectedDomain, bool expectedIsUrl, bool expectedIsFile)
    {
        var req = UrlRequest.Parse(input);
        req.IsUrl.Should().Be(expectedIsUrl);
        req.IsFile.Should().Be(expectedIsFile);
        req.Domain.Should().Be(expectedDomain);
        req.Url.Should().Be(input);
    }

    [Theory]
    [InlineData("C:\\Users\\test\\page.html", true)]
    [InlineData("C:\\path\\file.htm", true)]
    [InlineData("C:\\shortcut.url", true)]
    public void Parse_FileArguments(string input, bool expectedIsFile)
    {
        var req = UrlRequest.Parse(input);
        req.IsFile.Should().Be(expectedIsFile);
    }

    [Fact]
    public void Parse_EmptyString_ReturnsEmptyRequest()
    {
        var req = UrlRequest.Parse(string.Empty);
        req.IsUrl.Should().BeFalse();
        req.IsFile.Should().BeFalse();
        req.Domain.Should().BeNull();
    }

    [Fact]
    public void Parse_PreservesOriginalArgument()
    {
        const string url = "https://example.com/test";
        var req = UrlRequest.Parse(url);
        req.RawArgument.Should().Be(url);
    }
}
