using System;
using System.Collections.Generic;
using System.Windows;

namespace SoliloClock.Services;

public sealed record ThemeDefinition(
    string Id,
    string DisplayName,
    string Description,
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
        new ThemeDefinition("pink", "Pink", "Rose, lilac and soft sparkles.", "Themes/Pink.xaml", true, false, false),
        new ThemeDefinition("blue", "Blue", "Energetic cobalt and sky blue.", "Themes/Blue.xaml", false, false, false),
        new ThemeDefinition("black", "Black", "Moody graphite and steel.", "Themes/Black.xaml", false, false, false),
        new ThemeDefinition("neon", "Neon", "Retro-futuristic cyan and magenta glow.", "Themes/Neon.xaml", false, true, false),
        new ThemeDefinition("transparent", "Transparent", "A floating desktop clock with adjustable opacity.", "Themes/Transparent.xaml", false, false, true),
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
