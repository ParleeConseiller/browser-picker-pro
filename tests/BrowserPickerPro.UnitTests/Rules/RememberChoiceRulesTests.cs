// Browser Picker Pro — Created by Parlee Conseiller, Inc.
// File: RememberChoiceRulesTests.cs
// Description: "Remember for this domain" rule creation and its interaction with the rules engine.
// Author: Jose-Jorge HERNANDEZ
// Company: Parlee Conseiller, Inc.
// Date: 2026-11-25
// Last edit date: 2026-11-25
// Version: 1.0.0
using BrowserPickerPro.Core.Enums;
using BrowserPickerPro.Core.Models;
using BrowserPickerPro.Rules.Engine;
using BrowserPickerPro.Rules.Services;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace BrowserPickerPro.UnitTests.Rules;

public class RememberChoiceRulesTests
{
    [Fact]
    public void Creates_a_domain_rule_with_www_stripped()
    {
        var rules = RememberChoiceRules.Upsert(Array.Empty<BrowserRule>(), "WWW.GitHub.com", "b1", "p1");

        var rule = rules.Should().ContainSingle().Subject;
        rule.Pattern.Should().Be("github.com");
        rule.RuleType.Should().Be(RuleType.Domain);
        rule.Action.Should().Be(RuleAction.OpenInBrowser);
        rule.TargetBrowserId.Should().Be("b1");
        rule.TargetProfileId.Should().Be("p1");
    }

    [Fact]
    public void Replaces_a_previous_remembered_rule_but_keeps_user_rules()
    {
        var userRule = new BrowserRule { Pattern = "github.com", RuleType = RuleType.Domain, Action = RuleAction.AskAlways };
        var first = RememberChoiceRules.Upsert(new[] { userRule }, "github.com", "old", null);
        var second = RememberChoiceRules.Upsert(first, "github.com", "new", null);

        second.Should().HaveCount(2);
        second.Should().Contain(userRule);
        second.Single(r => r.Description == RememberChoiceRules.Marker).TargetBrowserId.Should().Be("new");
    }

    [Fact]
    public void Remembered_rule_wins_over_the_AskAlways_rule_that_showed_the_picker()
    {
        var ask = new BrowserRule { Pattern = "github.com", RuleType = RuleType.Domain, Action = RuleAction.AskAlways };
        var rules = RememberChoiceRules.Upsert(new[] { ask }, "github.com", "b1", null);

        var engine = new RulesEngine(new RulesPersistenceService(NullLogger<RulesPersistenceService>.Instance),
                                     NullLogger<RulesEngine>.Instance);
        var match = engine.FindMatchingRule(UrlRequest.Parse("https://www.github.com/x"), rules);

        match!.Action.Should().Be(RuleAction.OpenInBrowser);
        match.TargetBrowserId.Should().Be("b1");
    }
}
