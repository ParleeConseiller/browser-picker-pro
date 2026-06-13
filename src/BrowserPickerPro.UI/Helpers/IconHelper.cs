// Browser Picker Pro — Created by Parlee Conseiller, Inc.
using System.Drawing;
using System.IO;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace BrowserPickerPro.UI.Helpers;

public static class IconHelper
{
    private static readonly Dictionary<string, ImageSource> _cache = new();

    public static ImageSource? GetBrowserIcon(string executablePath)
    {
        if (string.IsNullOrWhiteSpace(executablePath) || !File.Exists(executablePath))
            return null;

        if (_cache.TryGetValue(executablePath, out var cached))
            return cached;

        try
        {
            var icon = Icon.ExtractAssociatedIcon(executablePath);
            if (icon == null) return null;

            var source = Imaging.CreateBitmapSourceFromHIcon(
                icon.Handle,
                Int32Rect.Empty,
                BitmapSizeOptions.FromWidthAndHeight(64, 64));

            source.Freeze();
            _cache[executablePath] = source;
            return source;
        }
        catch
        {
            return null;
        }
    }

    public static void ClearCache() => _cache.Clear();
}
