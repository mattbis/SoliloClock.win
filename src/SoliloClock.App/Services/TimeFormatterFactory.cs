using System;
using System.Collections.Generic;

namespace SoliloClock.Services;

public sealed record TimeFormatterSelection(
    ITimeFormatter Formatter,
    string Locale,
    string MatchedLocale,
    bool UsedFallback,
    string? Diagnostic);

/// <summary>
/// Chooses the time grammar for an announcement language. M2.1 registers only
/// English; each language stage adds its own formatter here. A locale with no
/// grammar is announced in English and says so, rather than mixing languages
/// silently.
/// </summary>
public static class TimeFormatterFactory
{
    private static readonly (string Locale, Func<ITimeFormatter> Create)[] Registered =
    {
        ("en-GB", static () => new EnglishTimeFormatter()),
        ("en-US", static () => new EnglishTimeFormatter()),
        ("en-AU", static () => new EnglishTimeFormatter()),
        ("en", static () => new EnglishTimeFormatter()),
    };

    public static TimeFormatterSelection Resolve(string? locale)
    {
        var requested = LocaleRegistry.NormalizeTag(locale);

        var exact = Find(requested);
        if (exact is { } match)
        {
            return new TimeFormatterSelection(match.Create(), requested, match.Locale, false, null);
        }

        // en-CA should inherit English grammar rather than fall all the way back.
        var byLanguage = Find(LocaleRegistry.LanguageOf(requested));
        if (byLanguage is { } languageMatch)
        {
            return new TimeFormatterSelection(
                languageMatch.Create(),
                requested,
                languageMatch.Locale,
                false,
                null);
        }

        var fallback = Find(LocalizationCatalog.FallbackLocale)!.Value;
        return new TimeFormatterSelection(
            fallback.Create(),
            requested,
            fallback.Locale,
            true,
            $"No time grammar for '{requested}'; using {fallback.Locale}.");
    }

    public static bool HasGrammarFor(string? locale) => !Resolve(locale).UsedFallback;

    private static (string Locale, Func<ITimeFormatter> Create)? Find(string? locale)
    {
        if (string.IsNullOrWhiteSpace(locale))
        {
            return null;
        }

        foreach (var entry in Registered)
        {
            if (string.Equals(entry.Locale, locale, StringComparison.OrdinalIgnoreCase))
            {
                return entry;
            }
        }

        return null;
    }
}
