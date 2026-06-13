// Browser Picker Pro — Created by Parlee Conseiller, Inc.
using BrowserPickerPro.Rules.Matchers;
using FluentAssertions;
using Xunit;

namespace BrowserPickerPro.UnitTests.Rules;

public class DomainMatcherTests
{
    [Theory]
    [InlineData("https://youtube.com", "youtube.com", true)]
    [InlineData("https://www.youtube.com", "youtube.com", true)]
    [InlineData("https://music.youtube.com", "youtube.com", true)]
    [InlineData("https://youtube.com/watch?v=test", "youtube.com", true)]
    [InlineData("https://YOUTUBE.COM", "youtube.com", true)]
    [InlineData("https://notyoutube.com", "youtube.com", false)]
    [InlineData("https://google.com", "youtube.com", false)]
    [InlineData("https://evil-youtube.com", "youtube.com", false)]
    public void IsMatch_DomainVariants(string url, string pattern, bool expected)
    {
        DomainMatcher.IsMatch(url, pattern).Should().Be(expected);
    }

    [Theory]
    [InlineData("not-a-url", "youtube.com", false)]
    [InlineData("", "youtube.com", false)]
    public void IsMatch_InvalidUrl_ReturnsFalse(string url, string pattern, bool expected)
    {
        DomainMatcher.IsMatch(url, pattern).Should().Be(expected);
    }
}
