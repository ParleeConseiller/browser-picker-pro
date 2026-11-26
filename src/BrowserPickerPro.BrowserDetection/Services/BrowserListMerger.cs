// Browser Picker Pro — Created by Parlee Conseiller, Inc.
// File: BrowserListMerger.cs
// Description: Merges freshly detected browsers into the user's saved list, keeping the
//              user's ids, order, enabled flags and usage stats — and, since v2.0, the ids
//              of existing profiles, so "remember my choice" rules keep pointing at them.
// Author: Jose-Jorge HERNANDEZ
// Company: Parlee Conseiller, Inc.
// Date: 2026-11-25
// Last edit date: 2026-11-25
// Version: 1.0.0
using BrowserPickerPro.Core.Models;

namespace BrowserPickerPro.BrowserDetection.Services;

public static class BrowserListMerger
{
    /// <summary>
    /// Returns <paramref name="saved"/> updated with <paramref name="detected"/>:
    /// unknown browsers are appended (after the existing order), known ones get the
    /// detected version and profile list with stable profile ids.
    /// </summary>
    public static List<Browser> Merge(List<Browser> saved, List<Browser> detected)
    {
        if (saved.Count == 0) return detected;

        // New browsers go to the end of the picker instead of colliding with OrderIndex 0.
        var nextOrder = saved.Max(b => b.OrderIndex) + 1;

        foreach (var d in detected)
        {
            // The executable path is the identity of a browser across detections.
            var existing = saved.FirstOrDefault(s =>
                string.Equals(s.ExecutablePath, d.ExecutablePath, StringComparison.OrdinalIgnoreCase));

            if (existing == null)
            {
                d.OrderIndex = nextOrder++;
                saved.Add(d);
                continue;
            }

            existing.Version = d.Version;
            existing.Profiles = MergeProfiles(existing, d.Profiles);
        }
        return saved;
    }

    // Detection creates every profile with a brand-new Guid; reuse the old id (and the
    // user's enabled flag) when the same profile switch was already known.
    private static List<BrowserProfile> MergeProfiles(Browser existing, List<BrowserProfile> detected)
    {
        foreach (var profile in detected)
        {
            var old = existing.Profiles.FirstOrDefault(p =>
                string.Equals(p.ProfileArgument, profile.ProfileArgument, StringComparison.OrdinalIgnoreCase));
            if (old != null)
            {
                profile.Id = old.Id;
                profile.IsEnabled = old.IsEnabled;
            }
            // Detected profiles reference the throw-away detected browser; re-point them.
            profile.BrowserId = existing.Id;
        }
        return detected;
    }
}
