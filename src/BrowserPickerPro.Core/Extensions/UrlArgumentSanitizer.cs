// Browser Picker Pro — Created by Parlee Conseiller, Inc.
// File: UrlArgumentSanitizer.cs
// Description: Makes a URL safe to embed as ONE quoted argument on a browser's command
//              line. Without it a crafted link such as  https://x/" --renderer-cmd-prefix="calc
//              would break out of the quotes and inject browser switches (code execution
//              class of bug in Chromium-based browsers).
// Author: Jose-Jorge HERNANDEZ
// Company: Parlee Conseiller, Inc.
// Date: 2026-11-25
// Last edit date: 2026-11-25
// Version: 1.0.0
using System.Text;

namespace BrowserPickerPro.Core.Extensions;

public static class UrlArgumentSanitizer
{
    /// <summary>
    /// Returns the URL with quotes percent-encoded, control characters removed and
    /// trailing backslashes doubled (so they can't escape the closing quote), or
    /// <c>null</c> when the value must not be passed to a browser at all.
    /// </summary>
    public static string? Sanitize(string? url)
    {
        if (string.IsNullOrWhiteSpace(url)) return null;

        var trimmed = url.Trim();

        // A value starting with '-' or '/' would be read as a switch even inside quotes,
        // because the quotes are stripped by CommandLineToArgvW before the browser sees it.
        if (trimmed[0] == '-' || trimmed[0] == '/') return null;

        var sb = new StringBuilder(trimmed.Length + 8);
        foreach (var c in trimmed)
        {
            if (char.IsControl(c)) continue;          // CR/LF/TAB/NUL never belong in a URL
            if (c == '"') { sb.Append("%22"); continue; } // a raw quote would end the argument
            sb.Append(c);
        }

        // "C:\dir\" + closing quote would become an escaped quote (\"). Doubling the
        // trailing backslashes turns them back into literal backslashes.
        var trailing = 0;
        for (var i = sb.Length - 1; i >= 0 && sb[i] == '\\'; i--) trailing++;
        if (trailing > 0) sb.Append('\\', trailing);

        return sb.Length == 0 ? null : sb.ToString();
    }

    /// <summary>Builds "[profile args] "url"" for a browser, or null if the URL is rejected.</summary>
    public static string? BuildArguments(string url, string? profileArgument)
    {
        var safe = Sanitize(url);
        if (safe is null) return null;

        return string.IsNullOrWhiteSpace(profileArgument)
            ? $"\"{safe}\""
            : $"{profileArgument} \"{safe}\"";
    }
}
