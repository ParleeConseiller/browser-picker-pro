// Browser Picker Pro — Created by Parlee Conseiller, Inc.
using BrowserPickerPro.Core.Enums;
using BrowserPickerPro.Core.Models;
using BrowserPickerPro.Infrastructure.Services;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using System.IO;
using System.Text.Json;
using Xunit;

namespace BrowserPickerPro.UnitTests.Infrastructure;

public class SettingsServiceTests : IDisposable
{
    private readonly SettingsService _service;
    private readonly string _settingsPath;

    public SettingsServiceTests()
    {
        _service = new SettingsService(NullLogger<SettingsService>.Instance);
        _settingsPath = Path.Combine(_service.AppDataPath, "settings.json");
    }

    [Fact]
    public async Task LoadAsync_ReturnsValidSettings()
    {
        var settings = await _service.LoadAsync();
        settings.Should().NotBeNull();
        settings.EnableSecurityCheck.Should().BeFalse();
        Enum.IsDefined(settings.Theme).Should().BeTrue();
    }

    [Fact]
    public async Task SaveAndLoad_RoundTrip()
    {
        var original = await _service.LoadAsync();
        var originalTheme = original.Theme;
        original.Theme = AppTheme.Dark;
        original.EnableAnimations = false;

        await _service.SaveAsync(original);
        var loaded = await _service.LoadAsync();

        loaded.Theme.Should().Be(AppTheme.Dark);
        loaded.EnableAnimations.Should().BeFalse();

        // Restore
        original.Theme = originalTheme;
        original.EnableAnimations = true;
        await _service.SaveAsync(original);
    }

    [Fact]
    public async Task SaveBrowsersAsync_PersistsBrowserList()
    {
        var testBrowsersPath = Path.Combine(_service.AppDataPath, "browsers_test.json");
        var browsers = new List<Browser>
        {
            new() { Name = "TestChrome", ExecutablePath = "C:\\chrome.exe", Version = "124.0" },
            new() { Name = "TestFirefox", ExecutablePath = "C:\\firefox.exe", Version = "126.0" }
        };

        await _service.SaveBrowsersAsync(browsers);
        var loaded = await _service.LoadBrowsersAsync();

        loaded.Should().HaveCountGreaterThanOrEqualTo(2);
        loaded.Any(b => b.Name == "TestChrome").Should().BeTrue();
    }

    [Fact]
    public void AppDataPath_IsNotEmpty()
    {
        _service.AppDataPath.Should().NotBeNullOrWhiteSpace();
        _service.AppDataPath.Should().Contain("BrowserPickerPro");
    }

    public void Dispose() { }
}
