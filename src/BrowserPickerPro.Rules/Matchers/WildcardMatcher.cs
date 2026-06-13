// Browser Picker Pro — Created by Parlee Conseiller, Inc.
using System.Text.RegularExpressions;

namespace BrowserPickerPro.Rules.Matchers;

public static class WildcardMatcher
{
    public static bool IsMatch(string url, string pattern)
    {
        try
        {
            var regexPattern = "^" + Regex.Escape(pattern)
                .Replace(@"\*", ".*")
                .Replace(@"\?", ".") + "$";
            return Regex.IsMatch(url, regexPattern, RegexOptions.IgnoreCase);
        }
        catch
        {
            return false;
        }
    }
}
