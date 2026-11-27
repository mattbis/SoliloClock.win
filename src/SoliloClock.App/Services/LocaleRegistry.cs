using System;
using System.Collections.Generic;
using System.Globalization;

namespace SoliloClock.Services;

/// <summary>
/// Rollout order from the plan: infrastructure first, then languages grouped by
/// region starting in Europe. A locale only ships when its UI strings, time
/// grammar and phrase catalog exist; a provisional locale ships but is flagged
/// in the picker until a native speaker has reviewed the wording.
/// </summary>
public enum LocalizationStage
{
    Foundation,
    WesternEurope,
    CentralEasternEurope,
    NorthernEurope,
    MiddleEast,
    SouthAsia,
    EastAsia,
}

public sealed record LocaleDefinition(
    string Tag,
    string NativeName,
    string EnglishName,
    LocalizationStage Stage,
    bool HasUiStrings,
    bool HasTimeGrammar,
    bool HasPhraseCatalog,
    bool IsProvisional = false)
{
    public bool IsShipped => HasUiStrings && HasTimeGrammar;
}

/// <summary>
/// Single source of truth for which locales exist, what they are called and
/// how far they have got. Nothing here promises speech: that depends on an
/// installed Windows voice.
/// </summary>
public static class LocaleRegistry
{
    public const string FallbackTag = "en-GB";

    public static IReadOnlyList<LocaleDefinition> All { get; } = new[]
    {
        // Foundation: the reference locale every other catalog overlays.
        new LocaleDefinition("en-GB", "English", "English (United Kingdom)", LocalizationStage.Foundation, true, true, true),

        // M2.2 — Western Europe
        new LocaleDefinition("fr-FR", "Français", "French (France)", LocalizationStage.WesternEurope, true, true, true),
        // Provisional: the wording is machine-drafted, so the picker marks it as
        // a draft until a Welsh speaker has reviewed it.
        new LocaleDefinition("cy-GB", "Cymraeg", "Welsh (United Kingdom)", LocalizationStage.WesternEurope, true, true, true, IsProvisional: true),
        new LocaleDefinition("de-DE", "Deutsch", "German (Germany)", LocalizationStage.WesternEurope, true, true, true),
        new LocaleDefinition("es-ES", "Español", "Spanish (Spain)", LocalizationStage.WesternEurope, true, true, true),
        new LocaleDefinition("it-IT", "Italiano", "Italian (Italy)", LocalizationStage.WesternEurope, true, true, true),
        new LocaleDefinition("nl-NL", "Nederlands", "Dutch (Netherlands)", LocalizationStage.WesternEurope, true, true, true),
        new LocaleDefinition("pt-PT", "Português", "Portuguese (Portugal)", LocalizationStage.WesternEurope, true, true, true),

        // M2.3 — Central, Northern and Eastern Europe
        new LocaleDefinition("pl-PL", "Polski", "Polish (Poland)", LocalizationStage.CentralEasternEurope, true, true, true),
        new LocaleDefinition("ru-RU", "Русский", "Russian (Russia)", LocalizationStage.CentralEasternEurope, true, true, true),
        new LocaleDefinition("el-GR", "Ελληνικά", "Greek (Greece)", LocalizationStage.CentralEasternEurope, true, true, true),

        // M2.7 — the rest of the Windows voice list. Every one of these is
        // machine-drafted, so all are flagged until a native speaker reviews them.
        new LocaleDefinition("da-DK", "Dansk", "Danish (Denmark)", LocalizationStage.NorthernEurope, true, true, true, IsProvisional: true),
        new LocaleDefinition("sv-SE", "Svenska", "Swedish (Sweden)", LocalizationStage.NorthernEurope, true, true, true, IsProvisional: true),
        new LocaleDefinition("nb-NO", "Norsk bokmål", "Norwegian Bokmål (Norway)", LocalizationStage.NorthernEurope, true, true, true, IsProvisional: true),
        new LocaleDefinition("cs-CZ", "Čeština", "Czech (Czechia)", LocalizationStage.NorthernEurope, true, true, true, IsProvisional: true),
        new LocaleDefinition("tr-TR", "Türkçe", "Turkish (Türkiye)", LocalizationStage.NorthernEurope, true, true, true, IsProvisional: true),
        new LocaleDefinition("hu-HU", "Magyar", "Hungarian (Hungary)", LocalizationStage.NorthernEurope, true, true, true, IsProvisional: true),
        new LocaleDefinition("ro-RO", "Română", "Romanian (Romania)", LocalizationStage.NorthernEurope, true, true, true, IsProvisional: true),
        new LocaleDefinition("hr-HR", "Hrvatski", "Croatian (Croatia)", LocalizationStage.NorthernEurope, true, true, true, IsProvisional: true),
        new LocaleDefinition("bg-BG", "Български", "Bulgarian (Bulgaria)", LocalizationStage.NorthernEurope, true, true, true, IsProvisional: true),
        new LocaleDefinition("sk-SK", "Slovenčina", "Slovak (Slovakia)", LocalizationStage.NorthernEurope, true, true, true, IsProvisional: true),
        new LocaleDefinition("fi-FI", "Suomi", "Finnish (Finland)", LocalizationStage.NorthernEurope, true, true, true, IsProvisional: true),

        // M2.4 — Middle East (right-to-left). Machine-drafted, so flagged until a
        // native speaker reviews it.
        new LocaleDefinition("ar", "العربية", "Arabic", LocalizationStage.MiddleEast, true, true, true, IsProvisional: true),

        // M2.5 — South Asia. Machine-drafted, so flagged until a native speaker
        // reviews it.
        new LocaleDefinition("hi", "हिन्दी", "Hindi", LocalizationStage.SouthAsia, true, true, true, IsProvisional: true),

        // M2.6 — East Asia. Chinese ships as two independent catalogs; both are
        // machine-drafted and provisional.
        new LocaleDefinition("zh-Hans", "简体中文", "Chinese (Simplified)", LocalizationStage.EastAsia, true, true, true, IsProvisional: true),
        new LocaleDefinition("zh-Hant", "繁體中文", "Chinese (Traditional)", LocalizationStage.EastAsia, true, true, true, IsProvisional: true),
        new LocaleDefinition("ja-JP", "日本語", "Japanese (Japan)", LocalizationStage.EastAsia, true, true, true, IsProvisional: true),
        new LocaleDefinition("ko-KR", "한국어", "Korean (Korea)", LocalizationStage.EastAsia, true, true, true, IsProvisional: true),
    };

    public static LocaleDefinition? Find(string? tag)
    {
        var normalized = TryNormalizeTag(tag);
        if (normalized is null)
        {
            return null;
        }

        foreach (var locale in All)
        {
            if (string.Equals(locale.Tag, normalized, StringComparison.OrdinalIgnoreCase))
            {
                return locale;
            }
        }

        return null;
    }

    /// <summary>Returns a valid specific-culture tag, or the English fallback.</summary>
    public static string NormalizeTag(string? tag) => TryNormalizeTag(tag) ?? FallbackTag;

    /// <summary>Returns null for empty or unparseable tags instead of guessing.</summary>
    public static string? TryNormalizeTag(string? tag)
    {
        if (string.IsNullOrWhiteSpace(tag))
        {
            return null;
        }

        var trimmed = tag.Trim();
        if (trimmed.Length > 32)
        {
            return null;
        }

        try
        {
            return CultureInfo.GetCultureInfo(trimmed).Name;
        }
        catch (CultureNotFoundException)
        {
            return null;
        }
        catch (ArgumentException)
        {
            return null;
        }
    }

    public static string LanguageOf(string? tag)
    {
        try
        {
            return CultureInfo.GetCultureInfo(NormalizeTag(tag)).TwoLetterISOLanguageName;
        }
        catch (ArgumentException)
        {
            return CultureInfo.GetCultureInfo(FallbackTag).TwoLetterISOLanguageName;
        }
    }

    public static bool SharesLanguage(string? left, string? right) =>
        string.Equals(LanguageOf(left), LanguageOf(right), StringComparison.OrdinalIgnoreCase);

    /// <summary>
    /// Chinese has two independent shipped catalogs but one language code, so a
    /// region decides the script: Taiwan, Hong Kong and Macao use Traditional and
    /// every other Chinese region uses Simplified.
    /// </summary>
    public static bool UsesTraditionalChinese(string? tag)
    {
        var normalized = NormalizeTag(tag);
        if (!string.Equals(LanguageOf(normalized), "zh", StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        if (normalized.StartsWith("zh-Hant", StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        if (normalized.StartsWith("zh-Hans", StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        var separator = normalized.LastIndexOf('-');
        var region = separator >= 0 ? normalized[(separator + 1)..] : string.Empty;
        return region is "TW" or "HK" or "MO";
    }

    /// <summary>Native name when the locale is on the plan's list, otherwise the OS name.</summary>
    public static string GetDisplayName(string? tag)
    {
        var locale = Find(tag);
        if (locale is not null)
        {
            return locale.NativeName;
        }

        try
        {
            return CultureInfo.GetCultureInfo(NormalizeTag(tag)).NativeName;
        }
        catch (ArgumentException)
        {
            return NormalizeTag(tag);
        }
    }
}

public static class LocalizationCulture
{
    /// <summary>
    /// Builds the culture used for UI and announcement formatting. A talking
    /// clock should keep Gregorian wall-clock time and Western digits even when
    /// the locale's default calendar is not Gregorian (for example ar-SA).
    /// </summary>
    public static CultureInfo Create(string? tag)
    {
        CultureInfo culture;
        try
        {
            culture = CultureInfo.GetCultureInfo(LocaleRegistry.NormalizeTag(tag));
            if (culture.IsNeutralCulture)
            {
                culture = CultureInfo.CreateSpecificCulture(culture.Name);
            }
        }
        catch (ArgumentException)
        {
            return CultureInfo.GetCultureInfo(LocaleRegistry.FallbackTag);
        }

        try
        {
            if (culture.DateTimeFormat.Calendar is GregorianCalendar &&
                culture.NumberFormat.DigitSubstitution == DigitShapes.None)
            {
                return culture;
            }

            var clone = (CultureInfo)culture.Clone();
            clone.DateTimeFormat.Calendar = new GregorianCalendar();
            clone.NumberFormat.DigitSubstitution = DigitShapes.None;
            return clone;
        }
        catch (ArgumentException)
        {
            return culture;
        }
        catch (InvalidOperationException)
        {
            return culture;
        }
    }
}
