// Browser Picker Pro — Created by Parlee Conseiller, Inc.
using BrowserPickerPro.Core.Enums;
using BrowserPickerPro.Core.Models;
using BrowserPickerPro.Rules.Engine;
using BrowserPickerPro.Rules.Services;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace BrowserPickerPro.UnitTests.Rules;

public class RulesEngineTests
{
    private readonly RulesEngine _engine;

    public RulesEngineTests()
    {
        var persistence = new RulesPersistenceService(NullLogger<RulesPersistenceService>.Instance);
        _engine = new RulesEngine(persistence, NullLogger<RulesEngine>.Instance);
    }

    [Theory]
    [InlineData("https://youtube.com/watch?v=abc", "youtube.com", true)]
    [InlineData("https://www.youtube.com/watch", "youtube.com", true)]
    [InlineData("https://music.youtube.com", "youtube.com", true)]
    [InlineData("https://google.com", "youtube.com", false)]
    [InlineData("https://notyoutube.com", "youtube.com", false)]
    public void DomainRule_MatchesCorrectly(string url, string pattern, bool expected)
    {
        var rule = new BrowserRule { Pattern = pattern, RuleType = RuleType.Domain, IsEnabled = true };
        _engine.IsMatch(url, rule).Should().Be(expected);
    }

    [Theory]
    [InlineData("https://docs.google.com/spreadsheets", "*.google.com*", true)]
    [InlineData("https://mail.google.com", "*.google.com*", true)]
    [InlineData("https://google.com", "*.google.com*", false)]
    [InlineData("https://microsoft.com", "https://microsoft.com", true)]
    [InlineData("https://microsoft.com/other", "https://microsoft.com", false)]
    public void WildcardRule_MatchesCorrectly(string url, string pattern, bool expected)
    {
        var rule = new BrowserRule { Pattern = pattern, RuleType = RuleType.Wildcard, IsEnabled = true };
        _engine.IsMatch(url, rule).Should().Be(expected);
    }

    [Theory]
    [InlineData("https://banking.example.com", @"https://.*\.example\.com", true)]
    [InlineData("https://example.com", @"https://.*\.example\.com", false)]
    [InlineData("https://github.com/user/repo", @"https://github\.com/.*", true)]
    public void RegexRule_MatchesCorrectly(string url, string pattern, bool expected)
    {
        var rule = new BrowserRule { Pattern = pattern, RuleType = RuleType.Regex, IsEnabled = true };
        _engine.IsMatch(url, rule).Should().Be(expected);
    }

    [Fact]
    public void FindMatchingRule_ReturnsHighestPriorityMatch()
    {
        var rules = new[]
        {
            new BrowserRule { Pattern = "google.com", RuleType = RuleType.Domain, Priority = 100, IsEnabled = true, TargetBrowserId = "firefox" },
            new BrowserRule { Pattern = "google.com", RuleType = RuleType.Domain, Priority = 10,  IsEnabled = true, TargetBrowserId = "chrome" },
        };
        var request = UrlRequest.Parse("https://google.com");
        var match = _engine.FindMatchingRule(request, rules);

        match.Should().NotBeNull();
        match!.TargetBrowserId.Should().Be("chrome");
    }

    [Fact]
    public void FindMatchingRule_SkipsDisabledRules()
    {
        var rules = new[]
        {
            new BrowserRule { Pattern = "google.com", RuleType = RuleType.Domain, Priority = 10, IsEnabled = false, TargetBrowserId = "chrome" },
            new BrowserRule { Pattern = "google.com", RuleType = RuleType.Domain, Priority = 20, IsEnabled = true,  TargetBrowserId = "firefox" },
        };
        var request = UrlRequest.Parse("https://google.com");
        var match = _engine.FindMatchingRule(request, rules);

        match.Should().NotBeNull();
        match!.TargetBrowserId.Should().Be("firefox");
    }

    [Fact]
    public void FindMatchingRule_ReturnsNullWhenNoMatch()
    {
        var rules = new[]
        {
            new BrowserRule { Pattern = "youtube.com", RuleType = RuleType.Domain, IsEnabled = true }
        };
        var request = UrlRequest.Parse("https://google.com");
        _engine.FindMatchingRule(request, rules).Should().BeNull();
    }

    [Fact]
    public void ExactRule_MatchesOnlyExactUrl()
    {
        var rule = new BrowserRule { Pattern = "https://google.com", RuleType = RuleType.Exact, IsEnabled = true };
        _engine.IsMatch("https://google.com", rule).Should().BeTrue();
        _engine.IsMatch("https://google.com/search", rule).Should().BeFalse();
    }
}
