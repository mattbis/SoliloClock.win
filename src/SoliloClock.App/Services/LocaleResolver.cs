using System;
using System.Globalization;

namespace SoliloClock.Services;

/// <summary>
/// The decided locale plus what the user actually asked for, so the UI can say
/// "you wanted de-DE, this build speaks English" instead of silently switching.
/// </summary>
public sealed record LocaleSelection(
    string Locale,
    string RequestedLocale,
    bool FollowsSystem,
    bool LanguageDiffers);

/// <summary>
/// Chooses the interface and announcement language. The default is automatic:
/// the user's Windows display language, so no setup is required. An explicit
/// setting overrides it — for testing, or to announce in another language.
/// </summary>
public static class LocaleResolver
{
    public const string AutomaticValue = "auto";

    /// <summary>
    /// Captured once at startup so applying a different culture later cannot
    /// change what "the Windows language" means for this session.
    /// </summary>
    public static string SystemLocale { get; } = DetectSystemLocale();

    public static bool IsAutomatic(string? setting)
    {
        if (string.IsNullOrWhiteSpace(setting))
        {
            return true;
        }

        var trimmed = setting.Trim();
        return trimmed.Equals(AutomaticValue, StringComparison.OrdinalIgnoreCase)
            || trimmed.Equals("system", StringComparison.OrdinalIgnoreCase)
            || trimmed.Equals("os", StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Automatic resolves to the best shipped locale for the system language:
    /// an exact shipped tag, otherwise a shipped locale speaking the same
    /// language (fr-CA → fr-FR), otherwise English. An explicit tag is honoured
    /// as-is, so a missing catalog is reported rather than silently swapped.
    /// </summary>
    public static LocaleSelection ResolveUi(string? setting, string? systemLocale = null)
    {
        if (!IsAutomatic(setting))
        {
            var explicitTag = LocaleRegistry.TryNormalizeTag(setting);
            if (explicitTag is not null)
            {
                return new LocaleSelection(
                    explicitTag,
                    explicitTag,
                    FollowsSystem: false,
                    LanguageDiffers: false);
            }
        }

        var system = NormalizeSystemLocale(systemLocale);
        var effective = BestShipped(system);
        return new LocaleSelection(
            effective,
            system,
            FollowsSystem: true,
            LanguageDiffers: !LocaleRegistry.SharesLanguage(system, effective));
    }

    /// <summary>
    /// Automatic announcements follow the interface language, so a system in a
    /// language without bundled English/French grammar still speaks a language
    /// the app actually has.
    /// </summary>
    public static LocaleSelection ResolveSpeech(string? setting, string uiLocale)
    {
        if (!IsAutomatic(setting))
        {
            var explicitTag = LocaleRegistry.TryNormalizeTag(setting);
            if (explicitTag is not null)
            {
                return new LocaleSelection(
                    explicitTag,
                    explicitTag,
                    FollowsSystem: false,
                    LanguageDiffers: false);
            }
        }

        var ui = LocaleRegistry.NormalizeTag(uiLocale);
        return new LocaleSelection(ui, ui, FollowsSystem: true, LanguageDiffers: false);
    }

    public static string DetectSystemLocale()
    {
        foreach (var culture in new[] { CultureInfo.CurrentUICulture, CultureInfo.InstalledUICulture })
        {
            var tag = ToSpecificTag(culture);
            if (tag is not null)
            {
                return tag;
            }
        }

        return LocaleRegistry.FallbackTag;
    }

    private static string BestShipped(string requested)
    {
        var shipped = LocaleRegistry.All.Where(locale => locale.IsShipped).ToList();

        foreach (var locale in shipped)
        {
            if (string.Equals(locale.Tag, requested, StringComparison.OrdinalIgnoreCase))
            {
                return locale.Tag;
            }
        }

        // Chinese ships two scripts under one language code, so the region decides.
        if (string.Equals(LocaleRegistry.LanguageOf(requested), "zh", StringComparison.OrdinalIgnoreCase))
        {
            return LocaleRegistry.UsesTraditionalChinese(requested) ? "zh-Hant" : "zh-Hans";
        }

        foreach (var locale in shipped)
        {
            if (LocaleRegistry.SharesLanguage(locale.Tag, requested))
            {
                return locale.Tag;
            }
        }

        return LocaleRegistry.FallbackTag;
    }

    private static string NormalizeSystemLocale(string? systemLocale) =>
        systemLocale is null
            ? SystemLocale
            : LocaleRegistry.TryNormalizeTag(systemLocale) ?? LocaleRegistry.FallbackTag;

    private static string? ToSpecificTag(CultureInfo? culture)
    {
        if (culture is null || string.IsNullOrEmpty(culture.Name))
        {
            return null;
        }

        if (!culture.IsNeutralCulture)
        {
            return LocaleRegistry.TryNormalizeTag(culture.Name);
        }

        try
        {
            return LocaleRegistry.TryNormalizeTag(CultureInfo.CreateSpecificCulture(culture.Name).Name);
        }
        catch (ArgumentException)
        {
            return LocaleRegistry.TryNormalizeTag(culture.Name);
        }
    }
}

/// <summary>
/// Applies one culture to the current thread and to every thread started later,
/// so numbers, dates and the WPF language follow the chosen language at once.
/// </summary>
public static class CultureScope
{
    public static void Apply(CultureInfo culture)
    {
        CultureInfo.DefaultThreadCurrentCulture = culture;
        CultureInfo.DefaultThreadCurrentUICulture = culture;
        CultureInfo.CurrentCulture = culture;
        CultureInfo.CurrentUICulture = culture;
    }
}
