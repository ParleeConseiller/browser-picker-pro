// Browser Picker Pro — Created by Parlee Conseiller, Inc.
// File: BrowserListMergerTests.cs
// Description: Saved-vs-detected browser merge: user data and profile ids must survive re-detection.
// Author: Jose-Jorge HERNANDEZ
// Company: Parlee Conseiller, Inc.
// Date: 2026-11-25
// Last edit date: 2026-11-25
// Version: 1.0.0
using BrowserPickerPro.BrowserDetection.Services;
using BrowserPickerPro.Core.Models;
using FluentAssertions;
using Xunit;

namespace BrowserPickerPro.UnitTests.BrowserDetection;

public class BrowserListMergerTests
{
    private static Browser Make(string path, params string[] profileArgs) => new()
    {
        Name = Path.GetFileNameWithoutExtension(path),
        ExecutablePath = path,
        Profiles = profileArgs.Select(a => new BrowserProfile { ProfileArgument = a, Name = a }).ToList(),
    };

    [Fact]
    public void Empty_saved_list_returns_detected()
    {
        var detected = new List<Browser> { Make(@"C:\a.exe") };
        BrowserListMerger.Merge(new List<Browser>(), detected).Should().BeSameAs(detected);
    }

    [Fact]
    public void Existing_browser_keeps_its_id_order_and_profile_ids()
    {
        var saved = Make(@"C:\chrome.exe", "--profile-directory=Default");
        saved.OrderIndex = 3;
        saved.UsageCount = 7;
        var oldProfileId = saved.Profiles[0].Id;

        var detected = Make(@"C:\CHROME.exe", "--profile-directory=Default", "--profile-directory=\"Profile 2\"");
        detected.Version = "200.0";

        var merged = BrowserListMerger.Merge(new List<Browser> { saved }, new List<Browser> { detected });

        merged.Should().ContainSingle();
        var b = merged[0];
        b.Id.Should().Be(saved.Id);
        b.OrderIndex.Should().Be(3);
        b.UsageCount.Should().Be(7);
        b.Version.Should().Be("200.0");
        b.Profiles.Should().HaveCount(2);
        b.Profiles[0].Id.Should().Be(oldProfileId, "remembered rules reference profiles by id");
        b.Profiles.Should().OnlyContain(p => p.BrowserId == saved.Id);
    }

    [Fact]
    public void New_browser_is_appended_after_existing_order()
    {
        var saved = Make(@"C:\a.exe");
        saved.OrderIndex = 5;
        var merged = BrowserListMerger.Merge(new List<Browser> { saved }, new List<Browser> { Make(@"C:\b.exe") });

        merged.Should().HaveCount(2);
        merged[1].OrderIndex.Should().Be(6);
    }
}
