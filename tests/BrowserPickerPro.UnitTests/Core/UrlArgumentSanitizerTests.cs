// Browser Picker Pro — Created by Parlee Conseiller, Inc.
// File: UrlArgumentSanitizerTests.cs
// Description: Verifies URLs can't inject extra browser command-line switches.
// Author: Jose-Jorge HERNANDEZ
// Company: Parlee Conseiller, Inc.
// Date: 2026-11-25
// Last edit date: 2026-11-25
// Version: 1.0.0
using BrowserPickerPro.Core.Extensions;
using FluentAssertions;
using Xunit;

namespace BrowserPickerPro.UnitTests.Core;

public class UrlArgumentSanitizerTests
{
    [Fact]
    public void Normal_url_is_unchanged()
        => UrlArgumentSanitizer.Sanitize("https://example.com/a?b=c&d=e#f").Should().Be("https://example.com/a?b=c&d=e#f");

    [Fact]
    public void Quote_cannot_break_out_of_the_argument()
    {
        var args = UrlArgumentSanitizer.BuildArguments("https://x.test/\" --renderer-cmd-prefix=\"calc", null);
        // Exactly the two wrapping quotes remain; the injected one is percent-encoded.
        args.Should().Be("\"https://x.test/%22 --renderer-cmd-prefix=%22calc\"");
        args!.Count(c => c == '"').Should().Be(2);
    }

    [Theory]
    [InlineData("--gpu-launcher=calc")]
    [InlineData("-incognito")]
    [InlineData("/c calc")]
    [InlineData("   ")]
    [InlineData("")]
    public void Switch_like_or_empty_values_are_rejected(string value)
        => UrlArgumentSanitizer.Sanitize(value).Should().BeNull();

    [Fact]
    public void Control_characters_are_removed()
        => UrlArgumentSanitizer.Sanitize("https://a.test/\r\nx\t\0y").Should().Be("https://a.test/xy");

    [Fact]
    public void Trailing_backslashes_are_doubled_so_they_cannot_escape_the_closing_quote()
        => UrlArgumentSanitizer.BuildArguments(@"file:///C:\dir\", null).Should().Be("\"file:///C:\\dir\\\\\"");

    [Fact]
    public void Profile_switch_is_prepended()
        => UrlArgumentSanitizer.BuildArguments("https://a.test", "--profile-directory=\"Profile 1\"")
            .Should().Be("--profile-directory=\"Profile 1\" \"https://a.test\"");
}
