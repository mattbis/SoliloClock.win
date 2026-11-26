using System;
using System.Collections.Generic;
using System.Windows;

namespace SoliloClock.Services;

public sealed record ThemeDefinition(
    string Id,
    string NameKey,
    string DescriptionKey,
    string ResourceFile,
    bool Sparkles,
    bool Glow,
    bool IsTransparent);

/// <summary>
/// The five bundled themes. Names are purely aesthetic; any theme can be
/// combined with any voice or language.
/// </summary>
public static class ThemeCatalog
{
    public static IReadOnlyList<ThemeDefinition> All { get; } = new[]
    {
        new ThemeDefinition("pink", LocalizationKeys.ThemePinkName, LocalizationKeys.ThemePinkDescription, "Themes/Pink.xaml", true, false, false),
        new ThemeDefinition("blue", LocalizationKeys.ThemeBlueName, LocalizationKeys.ThemeBlueDescription, "Themes/Blue.xaml", false, false, false),
        new ThemeDefinition("black", LocalizationKeys.ThemeBlackName, LocalizationKeys.ThemeBlackDescription, "Themes/Black.xaml", false, false, false),
        new ThemeDefinition("neon", LocalizationKeys.ThemeNeonName, LocalizationKeys.ThemeNeonDescription, "Themes/Neon.xaml", false, true, false),
        new ThemeDefinition("transparent", LocalizationKeys.ThemeTransparentName, LocalizationKeys.ThemeTransparentDescription, "Themes/Transparent.xaml", false, false, true),
    };

    public static ThemeDefinition Get(string? id)
    {
        if (!string.IsNullOrWhiteSpace(id))
        {
            foreach (var theme in All)
            {
                if (string.Equals(theme.Id, id, StringComparison.OrdinalIgnoreCase))
                {
                    return theme;
                }
            }
        }

        return All[0];
    }
}

public static class ThemeManager
{
    /// <summary>
    /// Replaces the theme dictionary in the application resources. The base
    /// styles dictionary stays at index 0; the theme dictionary is index 1.
    /// </summary>
    public static void Apply(ThemeDefinition theme)
    {
        var app = Application.Current;
        if (app is null)
        {
            return;
        }

        var dictionary = new ResourceDictionary
        {
            Source = new Uri(theme.ResourceFile, UriKind.Relative),
        };

        var merged = app.Resources.MergedDictionaries;
        if (merged.Count > 1)
        {
            merged[1] = dictionary;
        }
        else if (merged.Count == 1)
        {
            merged.Add(dictionary);
        }
        else
        {
            merged.Add(dictionary);
        }
    }
}
