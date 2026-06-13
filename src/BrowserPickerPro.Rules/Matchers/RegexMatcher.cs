// Browser Picker Pro — Created by Parlee Conseiller, Inc.
using System.Text.RegularExpressions;

namespace BrowserPickerPro.Rules.Matchers;

public static class RegexMatcher
{
    public static bool IsMatch(string url, string pattern)
    {
        try
        {
            return Regex.IsMatch(url, pattern, RegexOptions.IgnoreCase, TimeSpan.FromMilliseconds(500));
        }
        catch
        {
            return false;
        }
    }

    public static bool IsValidPattern(string pattern)
    {
        try
        {
            _ = new Regex(pattern, RegexOptions.None, TimeSpan.FromMilliseconds(100));
            return true;
        }
        catch
        {
            return false;
        }
    }
}
