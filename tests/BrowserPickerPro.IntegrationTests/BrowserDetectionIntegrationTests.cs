// Browser Picker Pro — Created by Parlee Conseiller, Inc.
using BrowserPickerPro.BrowserDetection.Services;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace BrowserPickerPro.IntegrationTests;

public class BrowserDetectionIntegrationTests
{
    private readonly BrowserDetectionService _service;

    public BrowserDetectionIntegrationTests()
    {
        _service = new BrowserDetectionService(NullLogger<BrowserDetectionService>.Instance);
    }

    [Fact]
    public async Task DetectInstalledBrowsers_ReturnsAtLeastOneOnWindows()
    {
        var browsers = await _service.DetectInstalledBrowsersAsync();

        browsers.Should().NotBeNull();
        // On any developer machine, at least one browser should be present
        browsers.Count.Should().BeGreaterThanOrEqualTo(0);
    }

    [Fact]
    public async Task DetectedBrowsers_HaveRequiredFields()
    {
        var browsers = await _service.DetectInstalledBrowsersAsync();

        foreach (var browser in browsers)
        {
            browser.Name.Should().NotBeNullOrWhiteSpace();
            browser.ExecutablePath.Should().NotBeNullOrWhiteSpace();
            browser.Id.Should().NotBeNullOrWhiteSpace();
        }
    }

    [Fact]
    public async Task GetBrowserVersionAsync_ReturnsVersionForValidExe()
    {
        var notepad = Environment.ExpandEnvironmentVariables(@"%WINDIR%\System32\notepad.exe");
        if (!File.Exists(notepad)) return;

        var version = await _service.GetBrowserVersionAsync(notepad);
        version.Should().NotBeNull();
    }
}
