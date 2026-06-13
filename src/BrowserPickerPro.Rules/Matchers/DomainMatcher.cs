// Browser Picker Pro — Created by Parlee Conseiller, Inc.
namespace BrowserPickerPro.Rules.Matchers;

public static class DomainMatcher
{
    public static bool IsMatch(string url, string pattern)
    {
        if (!Uri.TryCreate(url, UriKind.Absolute, out var uri)) return false;

        var host = uri.Host.ToLowerInvariant().TrimStart('w').TrimStart('w').TrimStart('w').TrimStart('.');
        var normalizedHost = uri.Host.ToLowerInvariant();
        var normalizedPattern = pattern.ToLowerInvariant().TrimStart('.');

        if (normalizedPattern.StartsWith("www."))
            normalizedPattern = normalizedPattern[4..];

        var hostWithoutWww = normalizedHost.StartsWith("www.")
            ? normalizedHost[4..]
            : normalizedHost;

        return hostWithoutWww == normalizedPattern ||
               normalizedHost == normalizedPattern ||
               hostWithoutWww.EndsWith("." + normalizedPattern);
    }
}
