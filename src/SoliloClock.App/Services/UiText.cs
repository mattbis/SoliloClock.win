using System;
using System.Collections.Generic;
using System.Globalization;
using System.Windows;
using System.Windows.Markup;

namespace SoliloClock.Services;

/// <summary>
/// One language's interface strings, plus the culture used for numbers, dates
/// and layout direction. Lookups never throw: a missing key surfaces as the key
/// itself so a gap is visible rather than silently blank.
/// </summary>
public sealed class UiText
{
    private readonly IReadOnlyDictionary<string, string> _strings;

    public UiText(
        string locale,
        string requestedLocale,
        CultureInfo culture,
        IReadOnlyDictionary<string, string> strings,
        bool usedFallback,
        IReadOnlyList<string> diagnostics)
    {
        Locale = locale;
        RequestedLocale = requestedLocale;
        Culture = culture ?? throw new ArgumentNullException(nameof(culture));
        _strings = strings ?? throw new ArgumentNullException(nameof(strings));
        UsedFallback = usedFallback;
        Diagnostics = diagnostics ?? Array.Empty<string>();
        WpfLanguage = LanguageFor(locale);
    }

    /// <summary>Locale actually in use (after any fallback).</summary>
    public string Locale { get; }

    /// <summary>Locale the settings asked for, kept for honest status messages.</summary>
    public string RequestedLocale { get; }

    public CultureInfo Culture { get; }

    public bool UsedFallback { get; }

    public IReadOnlyList<string> Diagnostics { get; }

    /// <summary>Attach this to <c>FrameworkElement.Language</c> so WPF formats with the UI culture.</summary>
    public XmlLanguage WpfLanguage { get; }

    public bool IsRightToLeft
    {
        get
        {
            try
            {
                return Culture.TextInfo.IsRightToLeft;
            }
            catch (NotSupportedException)
            {
                return false;
            }
        }
    }

    public FlowDirection FlowDirection =>
        IsRightToLeft ? FlowDirection.RightToLeft : FlowDirection.LeftToRight;

    public string this[string key] => Get(key);

    public string Get(string key)
    {
        if (_strings.TryGetValue(key, out var value) && !string.IsNullOrWhiteSpace(value))
        {
            return value;
        }

        return key;
    }

    public string Format(string key, params object?[] arguments) =>
        string.Format(Culture, Get(key), arguments);

    public bool Contains(string key) => _strings.ContainsKey(key);

    private static XmlLanguage LanguageFor(string locale)
    {
        try
        {
            return XmlLanguage.GetLanguage(locale);
        }
        catch (ArgumentException)
        {
            return XmlLanguage.GetLanguage(LocalizationCatalog.FallbackLocale);
        }
    }
}
