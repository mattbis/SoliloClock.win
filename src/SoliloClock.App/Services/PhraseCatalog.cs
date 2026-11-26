using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using SoliloClock.Models;

namespace SoliloClock.Services;

public sealed record PhraseLoadResult(
    IReadOnlyList<Phrase> Phrases,
    IReadOnlyList<string> Diagnostics,
    bool UsedFallback,
    string Locale);

/// <summary>
/// Loads phrase catalogs from Resources/Phrases/&lt;locale&gt;.json. Invalid
/// entries are skipped with a diagnostic instead of failing the whole catalog.
/// </summary>
public static class PhraseCatalog
{
    private const string FallbackLocale = "en-GB";
    private const int MaxDiagnostics = 25;

    public static string DefaultDirectory =>
        Path.Combine(AppContext.BaseDirectory, "Resources", "Phrases");

    public static PhraseLoadResult LoadDefault(string? locale = null) =>
        Load(DefaultDirectory, locale ?? FallbackLocale);

    public static PhraseLoadResult Load(string directory, string locale)
    {
        var diagnostics = new List<string>();
        var requested = string.IsNullOrWhiteSpace(locale) ? FallbackLocale : locale;
        var path = Path.Combine(directory, requested + ".json");
        var usedFallback = false;

        if (!File.Exists(path))
        {
            usedFallback = true;
            path = Path.Combine(directory, FallbackLocale + ".json");
            diagnostics.Add($"No phrase catalog for '{requested}'; using {FallbackLocale}.");
        }

        if (!File.Exists(path))
        {
            diagnostics.Add($"No phrase catalog found in '{directory}'.");
            return new PhraseLoadResult(Array.Empty<Phrase>(), diagnostics, usedFallback, requested);
        }

        try
        {
            using var document = JsonDocument.Parse(File.ReadAllText(path));
            var root = document.RootElement;

            if (root.ValueKind != JsonValueKind.Object ||
                !root.TryGetProperty("phrases", out var phrasesElement) ||
                phrasesElement.ValueKind != JsonValueKind.Array)
            {
                diagnostics.Add($"{Path.GetFileName(path)} is not a valid phrase catalog.");
                return new PhraseLoadResult(Array.Empty<Phrase>(), diagnostics, usedFallback, requested);
            }

            var phrases = new List<Phrase>();
            var ids = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var skipped = 0;
            var index = 0;

            foreach (var entry in phrasesElement.EnumerateArray())
            {
                index++;
                if (phrases.Count + skipped >= 10_000)
                {
                    diagnostics.Add("Catalog entry limit reached; remaining entries ignored.");
                    break;
                }

                if (TryReadPhrase(entry, ids, out var phrase, out var error))
                {
                    phrases.Add(phrase);
                    ids.Add(phrase.Id);
                }
                else
                {
                    skipped++;
                    if (diagnostics.Count < MaxDiagnostics)
                    {
                        diagnostics.Add($"Skipped entry {index}: {error}");
                    }
                }
            }

            if (skipped > 0)
            {
                diagnostics.Add($"{skipped} invalid phrase(s) skipped.");
            }

            return new PhraseLoadResult(phrases, diagnostics, usedFallback, requested);
        }
        catch (Exception ex) when (ex is JsonException or IOException or UnauthorizedAccessException)
        {
            diagnostics.Add($"Could not read phrase catalog: {ex.Message}");
            return new PhraseLoadResult(Array.Empty<Phrase>(), diagnostics, usedFallback, requested);
        }
    }

    private static bool TryReadPhrase(
        JsonElement entry,
        HashSet<string> ids,
        out Phrase phrase,
        out string error)
    {
        phrase = null!;
        error = string.Empty;

        if (entry.ValueKind != JsonValueKind.Object)
        {
            error = "entry is not an object";
            return false;
        }

        var id = GetString(entry, "id");
        if (string.IsNullOrWhiteSpace(id))
        {
            error = "missing id";
            return false;
        }

        if (!ids.Add(id))
        {
            error = $"duplicate id '{id}'";
            return false;
        }

        ids.Remove(id); // the caller re-adds on success

        var text = GetString(entry, "text");
        if (string.IsNullOrWhiteSpace(text))
        {
            error = $"'{id}' has empty text";
            return false;
        }

        if (!PhraseCategories.TryParse(GetString(entry, "category"), out var category))
        {
            error = $"'{id}' has unsupported category";
            return false;
        }

        if (!ContentTones.TryParse(GetString(entry, "tone"), out var tone))
        {
            error = $"'{id}' has unsupported tone";
            return false;
        }

        var weight = 1;
        if (entry.TryGetProperty("weight", out var weightElement) && weightElement.ValueKind != JsonValueKind.Null)
        {
            if (weightElement.ValueKind != JsonValueKind.Number || !weightElement.TryGetInt32(out weight) || weight <= 0)
            {
                error = $"'{id}' has invalid weight";
                return false;
            }
        }

        DayPart? timeWindow = null;
        var windowText = GetString(entry, "timeWindow");
        if (!string.IsNullOrWhiteSpace(windowText))
        {
            if (!PhraseCategories.TryParseDayPart(windowText, out var parsedWindow))
            {
                error = $"'{id}' has unsupported timeWindow";
                return false;
            }

            timeWindow = parsedWindow;
        }

        int? minActiveMinutes = null;
        if (entry.TryGetProperty("minActiveMinutes", out var minElement) && minElement.ValueKind != JsonValueKind.Null)
        {
            if (minElement.ValueKind != JsonValueKind.Number || !minElement.TryGetInt32(out var minutes) || minutes < 0)
            {
                error = $"'{id}' has invalid minActiveMinutes";
                return false;
            }

            minActiveMinutes = minutes;
        }

        var familyFriendly = true;
        if (entry.TryGetProperty("familyFriendly", out var familyElement) && familyElement.ValueKind != JsonValueKind.Null)
        {
            if (familyElement.ValueKind is not (JsonValueKind.True or JsonValueKind.False))
            {
                error = $"'{id}' has invalid familyFriendly";
                return false;
            }

            familyFriendly = familyElement.GetBoolean();
        }

        phrase = new Phrase(id, text, category, tone, weight, timeWindow, minActiveMinutes, familyFriendly);
        return true;
    }

    private static string? GetString(JsonElement entry, string name) =>
        entry.TryGetProperty(name, out var element) && element.ValueKind == JsonValueKind.String
            ? element.GetString()
            : null;
}
