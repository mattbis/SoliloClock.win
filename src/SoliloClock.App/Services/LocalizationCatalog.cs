using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;

namespace SoliloClock.Services;

public sealed record LocalizationLoadResult(
    IReadOnlyDictionary<string, string> Strings,
    IReadOnlyList<string> Diagnostics,
    bool UsedFallback,
    string Locale,
    string RequestedLocale);

/// <summary>
/// Loads <c>Resources/Localization/&lt;locale&gt;.json</c>. English is always
/// loaded first and the requested locale is layered on top, so a partial
/// translation keeps the reviewed English wording for keys it has not covered
/// instead of showing blanks.
/// </summary>
public static class LocalizationCatalog
{
    public const string FallbackLocale = "en-GB";
    private const int MaxDiagnostics = 25;

    public static string DefaultDirectory =>
        Path.Combine(AppContext.BaseDirectory, "Resources", "Localization");

    public static LocalizationLoadResult LoadDefault() => Load(DefaultDirectory, FallbackLocale);

    public static UiText CreateUiText(string? locale) => CreateUiText(DefaultDirectory, locale);

    public static UiText CreateUiText(string directory, string? locale)
    {
        var result = Load(directory, locale);
        var culture = LocalizationCulture.Create(result.Locale);
        return new UiText(
            result.Locale,
            result.RequestedLocale,
            culture,
            result.Strings,
            result.UsedFallback,
            result.Diagnostics);
    }

    public static LocalizationLoadResult Load(string directory, string? locale)
    {
        var diagnostics = new List<string>();
        var requested = LocaleRegistry.NormalizeTag(locale);
        var strings = new Dictionary<string, string>(StringComparer.Ordinal);
        var usedFallback = false;

        ReadCatalog(Path.Combine(directory, FallbackLocale + ".json"), strings, diagnostics);

        if (!string.Equals(requested, FallbackLocale, StringComparison.OrdinalIgnoreCase))
        {
            var path = Path.Combine(directory, requested + ".json");
            if (!File.Exists(path))
            {
                usedFallback = true;
                AddDiagnostic(diagnostics, $"No interface strings for '{requested}'; using {FallbackLocale}.");
            }
            else
            {
                var localized = new Dictionary<string, string>(StringComparer.Ordinal);
                if (ReadCatalog(path, localized, diagnostics))
                {
                    foreach (var pair in localized)
                    {
                        strings[pair.Key] = pair.Value;
                    }
                }
            }
        }

        return new LocalizationLoadResult(
            strings,
            diagnostics,
            usedFallback,
            usedFallback ? FallbackLocale : requested,
            requested);
    }

    private static bool ReadCatalog(
        string path,
        IDictionary<string, string> target,
        ICollection<string> diagnostics)
    {
        if (!File.Exists(path))
        {
            AddDiagnostic(diagnostics, $"Localization file '{Path.GetFileName(path)}' is missing.");
            return false;
        }

        try
        {
            using var document = JsonDocument.Parse(File.ReadAllText(path));
            var root = document.RootElement;

            if (root.ValueKind != JsonValueKind.Object ||
                !root.TryGetProperty("strings", out var entries) ||
                entries.ValueKind != JsonValueKind.Object)
            {
                AddDiagnostic(diagnostics, $"{Path.GetFileName(path)} is not a valid localization catalog.");
                return false;
            }

            var skipped = 0;
            foreach (var entry in entries.EnumerateObject())
            {
                var value = entry.Value.ValueKind == JsonValueKind.String
                    ? entry.Value.GetString()
                    : null;

                if (string.IsNullOrWhiteSpace(value))
                {
                    skipped++;
                    AddDiagnostic(diagnostics, $"{Path.GetFileName(path)}: '{entry.Name}' has no text.");
                    continue;
                }

                target[entry.Name] = value!;
            }

            if (skipped > 0)
            {
                AddDiagnostic(diagnostics, $"{skipped} empty UI string(s) skipped in {Path.GetFileName(path)}.");
            }

            return true;
        }
        catch (Exception ex) when (ex is JsonException or IOException or UnauthorizedAccessException)
        {
            AddDiagnostic(diagnostics, $"Could not read {Path.GetFileName(path)}: {ex.Message}");
            return false;
        }
    }

    private static void AddDiagnostic(ICollection<string> diagnostics, string message)
    {
        if (diagnostics.Count < MaxDiagnostics)
        {
            diagnostics.Add(message);
        }
    }
}
