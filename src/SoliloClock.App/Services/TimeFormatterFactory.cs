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
        ("pl-PL", static () => new PolishTimeFormatter()),
        ("pl", static () => new PolishTimeFormatter()),
        ("ru-RU", static () => new RussianTimeFormatter()),
        ("ru", static () => new RussianTimeFormatter()),
        ("el-GR", static () => new GreekTimeFormatter()),
        ("el", static () => new GreekTimeFormatter()),
        ("da-DK", static () => new DanishTimeFormatter()),
        ("da", static () => new DanishTimeFormatter()),
        ("sv-SE", static () => new SwedishTimeFormatter()),
        ("sv", static () => new SwedishTimeFormatter()),
        ("nb-NO", static () => new NorwegianTimeFormatter()),
        ("nb", static () => new NorwegianTimeFormatter()),
        ("no", static () => new NorwegianTimeFormatter()),
        ("cs-CZ", static () => new CzechTimeFormatter()),
        ("cs", static () => new CzechTimeFormatter()),
        ("tr-TR", static () => new TurkishTimeFormatter()),
        ("tr", static () => new TurkishTimeFormatter()),
        ("hu-HU", static () => new HungarianTimeFormatter()),
        ("hu", static () => new HungarianTimeFormatter()),
        ("ro-RO", static () => new RomanianTimeFormatter()),
        ("ro", static () => new RomanianTimeFormatter()),
        ("hr-HR", static () => new CroatianTimeFormatter()),
        ("hr", static () => new CroatianTimeFormatter()),
        ("bg-BG", static () => new BulgarianTimeFormatter()),
        ("bg", static () => new BulgarianTimeFormatter()),
        ("sk-SK", static () => new SlovakTimeFormatter()),
        ("sk", static () => new SlovakTimeFormatter()),
        ("fi-FI", static () => new FinnishTimeFormatter()),
        ("fi", static () => new FinnishTimeFormatter()),
        ("sl-SI", static () => new SlovenianTimeFormatter()),
        ("sl", static () => new SlovenianTimeFormatter()),
        ("ca-ES", static () => new CatalanTimeFormatter()),
        ("ca", static () => new CatalanTimeFormatter()),
        ("id-ID", static () => new IndonesianTimeFormatter()),
        ("id", static () => new IndonesianTimeFormatter()),
        ("ms-MY", static () => new MalayTimeFormatter()),
        ("ms", static () => new MalayTimeFormatter()),
        ("he-IL", static () => new HebrewTimeFormatter()),
        ("he", static () => new HebrewTimeFormatter()),
        ("th-TH", static () => new ThaiTimeFormatter()),
        ("th", static () => new ThaiTimeFormatter()),
        ("vi-VN", static () => new VietnameseTimeFormatter()),
        ("vi", static () => new VietnameseTimeFormatter()),
        ("ta-IN", static () => new TamilTimeFormatter()),
        ("ta", static () => new TamilTimeFormatter()),
        ("ar", static () => new ArabicTimeFormatter()),
        ("zh-Hans", static () => new ChineseTimeFormatter()),
        ("zh-Hant", static () => new ChineseTimeFormatter(traditional: true)),
        ("ja-JP", static () => new JapaneseTimeFormatter()),
        ("ja", static () => new JapaneseTimeFormatter()),
        ("hi-IN", static () => new HindiTimeFormatter()),
        ("hi", static () => new HindiTimeFormatter()),
        ("ko-KR", static () => new KoreanTimeFormatter()),
        ("ko", static () => new KoreanTimeFormatter()),
    };

    public static TimeFormatterSelection Resolve(string? locale)
    {
        var requested = LocaleRegistry.NormalizeTag(locale);

        var exact = Find(requested);
        if (exact is { } match)
        {
            return new TimeFormatterSelection(match.Create(), requested, match.Locale, false, null);
        }

        // Chinese shares one language code across two scripts, so the region picks
        // the catalog rather than the first Chinese locale in the list.
        if (string.Equals(LocaleRegistry.LanguageOf(requested), "zh", StringComparison.OrdinalIgnoreCase))
        {
            var script = LocaleRegistry.UsesTraditionalChinese(requested) ? "zh-Hant" : "zh-Hans";
            var chinese = Find(script)!.Value;
            return new TimeFormatterSelection(chinese.Create(), requested, chinese.Locale, false, null);
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
