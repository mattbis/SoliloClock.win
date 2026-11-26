using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Windows;
using SoliloClock.Models;
using SoliloClock.Services;
using Xunit;

namespace SoliloClock.Tests;

public sealed class LocalizationTests
{
    [Fact]
    public void EnglishCatalog_CoversEveryKeyUsedByTheApp()
    {
        var catalog = LocalizationCatalog.LoadDefault();

        Assert.False(catalog.UsedFallback);
        Assert.Equal("en-GB", catalog.Locale);

        var keys = typeof(LocalizationKeys)
            .GetFields(BindingFlags.Public | BindingFlags.Static)
            .Where(field => field.IsLiteral && field.FieldType == typeof(string))
            .Select(field => (string)field.GetRawConstantValue()!)
            .ToList();

        Assert.NotEmpty(keys);
        foreach (var key in keys)
        {
            Assert.True(catalog.Strings.ContainsKey(key), $"missing UI string: {key}");
            Assert.False(string.IsNullOrWhiteSpace(catalog.Strings[key]), $"empty UI string: {key}");
        }
    }

    [Fact]
    public void UnknownLocale_FallsBackToEnglishWithADiagnostic()
    {
        var result = LocalizationCatalog.Load(LocalizationCatalog.DefaultDirectory, "tr-TR");

        Assert.True(result.UsedFallback);
        Assert.NotEmpty(result.Diagnostics);
        Assert.Equal("en-GB", result.Locale);
        Assert.Equal("tr-TR", result.RequestedLocale);
        Assert.Equal("SoliloClock", result.Strings[LocalizationKeys.AppName]);
    }

    [Fact]
    public void MissingDirectory_ReportsDiagnosticsInsteadOfThrowing()
    {
        var directory = Path.Combine(Path.GetTempPath(), "soliloclock-missing-" + Guid.NewGuid().ToString("N"));

        var result = LocalizationCatalog.Load(directory, "en-GB");

        Assert.Empty(result.Strings);
        Assert.NotEmpty(result.Diagnostics);
    }

    [Fact]
    public void UiText_UsesRightToLeftForArabic()
    {
        var text = new UiText(
            "ar",
            "ar",
            LocalizationCulture.Create("ar"),
            new Dictionary<string, string>(),
            usedFallback: false,
            Array.Empty<string>());

        Assert.True(text.IsRightToLeft);
        Assert.Equal(FlowDirection.RightToLeft, text.FlowDirection);
    }

    [Fact]
    public void UiText_ReturnsTheKeyInsteadOfBlankText()
    {
        var text = LocalizationCatalog.CreateUiText("en-GB");

        Assert.Equal("missing.key", text["missing.key"]);
        Assert.Equal("SoliloClock", text[LocalizationKeys.AppName]);
    }

    [Fact]
    public void ArabicCulture_KeepsGregorianWallClockTime()
    {
        var culture = LocalizationCulture.Create("ar-SA");
        var stamp = new DateTime(2026, 11, 26, 19, 45, 0);

        Assert.IsType<GregorianCalendar>(culture.DateTimeFormat.Calendar);
        Assert.Equal("2026-11-26", stamp.ToString("yyyy-MM-dd", culture));
        // Talked clock policy: the interface and clock face keep Western digits.
        Assert.Equal(DigitShapes.None, culture.NumberFormat.DigitSubstitution);
    }

    [Fact]
    public void UiText_ReportsFallbackWhenTheRequestedLanguageDiffers()
    {
        var sameLanguage = LocalizationCatalog.CreateUiText("fr-FR", "fr-CA");

        Assert.False(sameLanguage.UsedFallback);
        Assert.Equal("fr-CA", sameLanguage.RequestedLocale);
        Assert.Equal("fr-FR", sameLanguage.Locale);

        var otherLanguage = LocalizationCatalog.CreateUiText("en-GB", "de-DE");

        Assert.True(otherLanguage.UsedFallback);
        Assert.Equal("de-DE", otherLanguage.RequestedLocale);
        Assert.Equal("en-GB", otherLanguage.Locale);
    }

    [Fact]
    public void Registry_CoversThePlannedLocalesWithoutDuplicates()
    {
        var tags = LocaleRegistry.All.Select(locale => locale.Tag).ToList();

        Assert.Equal(tags.Count, tags.Distinct(StringComparer.OrdinalIgnoreCase).Count());

        var planned = new[]
        {
            "en-GB", "fr-FR", "de-DE", "es-ES", "it-IT", "nl-NL", "pt-PT",
            "pl-PL", "ru-RU", "el-GR", "ar", "hi", "zh-Hans", "zh-Hant", "ja-JP", "ko-KR",
            "cy-GB",
        };

        foreach (var tag in planned)
        {
            Assert.Contains(tags, existing => string.Equals(existing, tag, StringComparison.OrdinalIgnoreCase));
        }

        foreach (var locale in LocaleRegistry.All)
        {
            Assert.NotNull(LocaleRegistry.TryNormalizeTag(locale.Tag));
        }
    }

    [Fact]
    public void EveryShippedLocale_HasACatalogCoveringEveryKey()
    {
        var shipped = LocaleRegistry.All
            .Where(locale => locale.IsShipped)
            .Select(locale => locale.Tag)
            .ToArray();

        Assert.Equal(
            new[]
            {
                "en-GB", "fr-FR", "cy-GB", "de-DE", "es-ES", "it-IT", "nl-NL", "pt-PT",
                "pl-PL", "ru-RU", "el-GR", "ar", "hi", "zh-Hans", "zh-Hant", "ja-JP",
                "ko-KR",
            },
            shipped);

        foreach (var tag in shipped)
        {
            var result = LocalizationCatalog.Load(LocalizationCatalog.DefaultDirectory, tag);

            Assert.False(result.UsedFallback);
            Assert.Equal(tag, result.Locale);

            foreach (var key in AllKeys())
            {
                Assert.True(result.Strings.ContainsKey(key), $"{tag} is missing {key}");
                Assert.False(string.IsNullOrWhiteSpace(result.Strings[key]), $"{tag} has an empty {key}");
            }
        }
    }

    private static IReadOnlyList<string> AllKeys() =>
        typeof(LocalizationKeys)
            .GetFields(BindingFlags.Public | BindingFlags.Static)
            .Where(field => field.IsLiteral && field.FieldType == typeof(string))
            .Select(field => (string)field.GetRawConstantValue()!)
            .ToList();

    [Theory]
    [InlineData("", "en-GB")]
    [InlineData("   ", "en-GB")]
    [InlineData("not a locale", "en-GB")]
    [InlineData("fr-fr", "fr-FR")]
    [InlineData("de-DE", "de-DE")]
    public void NormalizeTag_ReturnsAValidTagOrTheFallback(string input, string expected)
    {
        Assert.Equal(expected, LocaleRegistry.NormalizeTag(input));
    }
}
