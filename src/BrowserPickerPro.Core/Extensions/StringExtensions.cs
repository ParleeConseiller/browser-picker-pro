// Browser Picker Pro — Created by Parlee Conseiller, Inc.
namespace BrowserPickerPro.Core.Extensions;

public static class StringExtensions
{
    public static bool IsNullOrWhiteSpace(this string? value) => string.IsNullOrWhiteSpace(value);

    public static string TruncateWithEllipsis(this string value, int maxLength)
    {
        if (string.IsNullOrEmpty(value) || value.Length <= maxLength)
            return value;
        return value[..(maxLength - 3)] + "...";
    }

    public static string ExtractDomain(this string url)
    {
        if (Uri.TryCreate(url, UriKind.Absolute, out var uri))
            return uri.Host.ToLowerInvariant();
        return url;
    }
}
