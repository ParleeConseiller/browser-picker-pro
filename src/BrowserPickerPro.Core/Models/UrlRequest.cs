// Browser Picker Pro — Created by Parlee Conseiller, Inc.
namespace BrowserPickerPro.Core.Models;

public class UrlRequest
{
    public string RawArgument { get; set; } = string.Empty;
    public string Url { get; set; } = string.Empty;
    public string? Domain { get; set; }
    public string? FilePath { get; set; }
    public bool IsFile { get; set; }
    public bool IsUrl { get; set; }
    public DateTime ReceivedAt { get; set; } = DateTime.UtcNow;

    public static UrlRequest Parse(string argument)
    {
        var req = new UrlRequest { RawArgument = argument };

        if (string.IsNullOrWhiteSpace(argument))
            return req;

        if (argument.StartsWith("http://", StringComparison.OrdinalIgnoreCase) ||
            argument.StartsWith("https://", StringComparison.OrdinalIgnoreCase) ||
            argument.StartsWith("ftp://", StringComparison.OrdinalIgnoreCase))
        {
            req.IsUrl = true;
            req.Url = argument;
            if (Uri.TryCreate(argument, UriKind.Absolute, out var uri))
                req.Domain = uri.Host.ToLowerInvariant();
        }
        else if (argument.EndsWith(".html", StringComparison.OrdinalIgnoreCase) ||
                 argument.EndsWith(".htm", StringComparison.OrdinalIgnoreCase) ||
                 argument.EndsWith(".url", StringComparison.OrdinalIgnoreCase))
        {
            req.IsFile = true;
            req.FilePath = argument;
            req.Url = $"file:///{argument.Replace('\\', '/')}";
        }
        else
        {
            req.IsUrl = true;
            req.Url = argument;
            if (Uri.TryCreate(argument, UriKind.Absolute, out var uri))
                req.Domain = uri.Host.ToLowerInvariant();
        }

        return req;
    }
}
