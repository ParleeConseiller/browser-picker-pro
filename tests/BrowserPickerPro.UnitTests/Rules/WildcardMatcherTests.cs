// Browser Picker Pro — Created by Parlee Conseiller, Inc.
using BrowserPickerPro.Rules.Matchers;
using FluentAssertions;
using Xunit;

namespace BrowserPickerPro.UnitTests.Rules;

public class WildcardMatcherTests
{
    [Theory]
    [InlineData("https://docs.google.com", "https://*.google.com*", true)]
    [InlineData("https://mail.google.com/inbox", "https://*.google.com*", true)]
    [InlineData("https://google.com", "https://*.google.com*", false)]
    [InlineData("https://microsoft.com/page", "https://microsoft.com*", true)]
    [InlineData("https://microsoft.com", "https://microsoft.com", true)]
    [InlineData("https://microsoft.com/extra", "https://microsoft.com", false)]
    [InlineData("any-text-here", "*", true)]
    [InlineData("https://a.b.c/d/e", "https://?.b.c*", true)]
    public void IsMatch_WildcardPatterns(string url, string pattern, bool expected)
    {
        WildcardMatcher.IsMatch(url, pattern).Should().Be(expected);
    }
}
