// Browser Picker Pro — Created by Parlee Conseiller, Inc.
using BrowserPickerPro.BrowserDetection.Services;
using BrowserPickerPro.Core.Interfaces;
using BrowserPickerPro.Infrastructure.Services;
using BrowserPickerPro.Rules.Engine;
using BrowserPickerPro.Rules.Services;
using BrowserPickerPro.Security.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Serilog;

namespace BrowserPickerPro.Infrastructure.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddBrowserPickerServices(this IServiceCollection services)
    {
        services.AddSingleton<ISettingsService, SettingsService>();
        services.AddSingleton<IBrowserDetectionService, BrowserDetectionService>();
        services.AddSingleton<IBrowserLauncher, BrowserLauncherService>();
        services.AddSingleton<IProtocolRegistrar, ProtocolRegistrarService>();

        services.AddSingleton<RulesPersistenceService>();
        services.AddSingleton<IRulesEngine, RulesEngine>();
        services.AddSingleton<SecurityOrchestrator>();

        services.AddHttpClient();

        return services;
    }

    public static IServiceCollection AddBrowserPickerLogging(this IServiceCollection services, string logDirectory)
    {
        Directory.CreateDirectory(logDirectory);

        var serilogLogger = new LoggerConfiguration()
            .MinimumLevel.Debug()
            .Enrich.FromLogContext()
            .Enrich.WithThreadId()
            .WriteTo.File(
                path: Path.Combine(logDirectory, "browserPickerPro-.log"),
                rollingInterval: RollingInterval.Day,
                retainedFileCountLimit: 7,
                outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level:u3}] {Message:lj}{NewLine}{Exception}")
            .CreateLogger();

        services.AddLogging(builder =>
        {
            builder.ClearProviders();
            builder.AddSerilog(serilogLogger, dispose: true);
        });

        return services;
    }
}
