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
/// Chooses the time grammar for an announcement language. Each language stage
/// adds its own formatter here; matching falls back from an exact tag to the
/// language, so fr-CA inherits French grammar. A locale with no grammar is
/// announced in English and says so, rather than mixing languages silently.
/// </summary>
public static class TimeFormatterFactory
{
    private static readonly (string Locale, Func<ITimeFormatter> Create)[] Registered =
    {
        ("en-GB", static () => new EnglishTimeFormatter()),
        ("en-US", static () => new EnglishTimeFormatter()),
        ("en-AU", static () => new EnglishTimeFormatter()),
        ("en", static () => new EnglishTimeFormatter()),
        ("fr-FR", static () => new FrenchTimeFormatter()),
        ("fr", static () => new FrenchTimeFormatter()),
        ("cy-GB", static () => new WelshTimeFormatter()),
        ("cy", static () => new WelshTimeFormatter()),
        ("de-DE", static () => new GermanTimeFormatter()),
        ("de", static () => new GermanTimeFormatter()),
        ("es-ES", static () => new SpanishTimeFormatter()),
        ("es", static () => new SpanishTimeFormatter()),
        ("it-IT", static () => new ItalianTimeFormatter()),
        ("it", static () => new ItalianTimeFormatter()),
        ("nl-NL", static () => new DutchTimeFormatter()),
        ("nl", static () => new DutchTimeFormatter()),
        ("pt-PT", static () => new PortugueseTimeFormatter()),
        ("pt", static () => new PortugueseTimeFormatter()),
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
