using System;
using System.IO;
using System.Linq;
using SoliloClock.Services;
using Xunit;

namespace SoliloClock.Tests;

/// <summary>
/// M2.4–M2.6: Arabic, Simplified and Traditional Chinese and Japanese ship as
/// complete packs, and because the wording is machine-drafted each one is marked
/// provisional until a native speaker reviews it.
/// </summary>
public sealed class MiddleEastAndEastAsiaTests
{
    private static string PhraseDirectory => Path.Combine(AppContext.BaseDirectory, "Phrases");

    [Theory]
    [InlineData("ar", "ArabicTimeFormatter")]
    [InlineData("zh-Hans", "ChineseTimeFormatter")]
    [InlineData("zh-Hant", "ChineseTimeFormatter")]
    [InlineData("ja-JP", "JapaneseTimeFormatter")]
    public void EachLanguage_ShipsACompleteProvisionalPack(string locale, string formatterType)
    {
        var ui = LocalizationCatalog.Load(LocalizationCatalog.DefaultDirectory, locale);

        Assert.False(ui.UsedFallback);
        Assert.Empty(ui.Diagnostics);
        Assert.Equal(locale, ui.Locale);

        var registered = LocaleRegistry.Find(locale);
        Assert.NotNull(registered);
        Assert.True(registered!.IsShipped);
        Assert.True(registered.HasPhraseCatalog);
        Assert.True(registered.IsProvisional);

        var selection = TimeFormatterFactory.Resolve(locale);
        Assert.Equal(formatterType, selection.Formatter.GetType().Name);
        Assert.False(selection.UsedFallback);

        var phrases = PhraseCatalog.Load(PhraseDirectory, locale);
        var english = PhraseCatalog.Load(PhraseDirectory, "en-GB");

        Assert.False(phrases.UsedFallback);
        Assert.Empty(phrases.Diagnostics);
        Assert.Equal(100, phrases.Phrases.Count);
        Assert.Equal(english.Phrases.Count, phrases.Phrases.Count);

        var englishById = english.Phrases.ToDictionary(
            phrase => phrase.Id,
            phrase => phrase.Text,
            StringComparer.OrdinalIgnoreCase);

        foreach (var phrase in phrases.Phrases)
        {
            Assert.True(englishById.ContainsKey(phrase.Id), $"{locale}: unexpected id {phrase.Id}");
            Assert.NotEqual(englishById[phrase.Id], phrase.Text);
        }

        foreach (var pair in english.Phrases.Zip(phrases.Phrases))
        {
            Assert.Equal(pair.First.Id, pair.Second.Id);
            Assert.Equal(pair.First.Category, pair.Second.Category);
            Assert.Equal(pair.First.Tone, pair.Second.Tone);
            Assert.Equal(pair.First.Weight, pair.Second.Weight);
            Assert.Equal(pair.First.TimeWindow, pair.Second.TimeWindow);
            Assert.Equal(pair.First.MinActiveMinutes, pair.Second.MinActiveMinutes);
        }
    }

    [Theory]
    [InlineData("ar", "Arabic")]
    [InlineData("zh-Hans", "Chinese (Simplified)")]
    [InlineData("zh-Hant", "Chinese (Traditional)")]
    [InlineData("ja-JP", "Japanese (Japan)")]
    public void ProvisionalLocales_ReportTheirEnglishNames(string locale, string englishName)
    {
        Assert.Equal(englishName, LocaleRegistry.Find(locale)!.EnglishName);
        Assert.NotEqual(LocaleRegistry.Find(locale)!.NativeName, englishName);
    }

    [Fact]
    public void ChineseRegions_ChooseTheMatchingScriptAutomatically()
    {
        Assert.Equal("zh-Hans", LocaleResolver.ResolveUi(null, "zh-CN").Locale);
        Assert.Equal("zh-Hans", LocaleResolver.ResolveUi(null, "zh-SG").Locale);

        var taiwan = LocaleResolver.ResolveUi(null, "zh-TW");
        Assert.Equal("zh-Hant", taiwan.Locale);
        Assert.False(taiwan.LanguageDiffers);

        Assert.Equal("zh-Hant", LocaleResolver.ResolveUi(null, "zh-HK").Locale);
    }

    [Fact]
    public void ChineseGrammar_FollowsTheSelectedScript()
    {
        var simplified = TimeFormatterFactory.Resolve("zh-CN");
        var traditional = TimeFormatterFactory.Resolve("zh-TW");

        Assert.Equal("zh-Hans", simplified.MatchedLocale);
        Assert.Equal("zh-Hant", traditional.MatchedLocale);

        var time = new DateTime(2026, 11, 26, 9, 45, 0);
        var options = new TimeFormatOptions(true, false);

        Assert.Equal("现在是差一刻十点。", simplified.Formatter.Format(time, options));
        Assert.Equal("現在是差一刻十點。", traditional.Formatter.Format(time, options));
    }

    [Theory]
    [InlineData(9, 0, "الساعة التاسعة.")]
    [InlineData(12, 0, "الظهر.")]
    [InlineData(0, 0, "منتصف الليل.")]
    [InlineData(9, 1, "الساعة التاسعة ودقيقة واحدة.")]
    [InlineData(9, 2, "الساعة التاسعة ودقيقتين.")]
    [InlineData(9, 5, "الساعة التاسعة وخمس دقائق.")]
    [InlineData(9, 15, "الساعة التاسعة والربع.")]
    [InlineData(9, 30, "الساعة التاسعة والنصف.")]
    [InlineData(9, 40, "الساعة العاشرة إلا عشرين دقيقة.")]
    [InlineData(9, 45, "الساعة العاشرة إلا ربعًا.")]
    public void Arabic_UsesFeminineHoursAndMinuteAgreement(int hour, int minute, string expected)
    {
        var formatter = new ArabicTimeFormatter();

        var spoken = formatter.Format(new DateTime(2026, 11, 26, hour, minute, 0), new TimeFormatOptions(true, false));

        Assert.Equal(expected, spoken);
    }

    [Fact]
    public void Arabic_Reads24HourTimeInWords()
    {
        var formatter = new ArabicTimeFormatter();

        var spoken = formatter.Format(new DateTime(2026, 11, 26, 19, 45, 0), new TimeFormatOptions(true, true));

        Assert.Equal("الساعة التاسعة عشرة وخمسة وأربعون دقيقة.", spoken);
    }

    [Theory]
    [InlineData(9, 0, "现在是九点。")]
    [InlineData(12, 0, "现在是中午十二点。")]
    [InlineData(0, 0, "现在是零点。")]
    [InlineData(9, 5, "现在是九点零五分。")]
    [InlineData(9, 15, "现在是九点一刻。")]
    [InlineData(9, 30, "现在是九点半。")]
    [InlineData(9, 40, "现在是九点四十分。")]
    [InlineData(9, 45, "现在是差一刻十点。")]
    public void SimplifiedChinese_UsesKeBanAndChaYiKe(int hour, int minute, string expected)
    {
        var formatter = new ChineseTimeFormatter();

        var spoken = formatter.Format(new DateTime(2026, 11, 26, hour, minute, 0), new TimeFormatOptions(true, false));

        Assert.Equal(expected, spoken);
    }

    [Fact]
    public void Chinese_Reads24HourAndExactTimeInWords()
    {
        var formatter = new ChineseTimeFormatter();
        var time = new DateTime(2026, 11, 26, 19, 45, 0);

        Assert.Equal(
            "现在是十九点四十五分。",
            formatter.Format(time, new TimeFormatOptions(true, true)));
        Assert.Equal(
            "现在是九点整。",
            formatter.Format(new DateTime(2026, 11, 26, 9, 0, 0), new TimeFormatOptions(false, false)));
    }

    [Theory]
    [InlineData(9, 0, "今は九時です。")]
    [InlineData(12, 0, "今は正午です。")]
    [InlineData(0, 0, "今は零時です。")]
    [InlineData(9, 5, "今は九時五分です。")]
    [InlineData(9, 30, "今は九時半です。")]
    [InlineData(9, 45, "今は九時四十五分です。")]
    public void Japanese_ReadsHourThenMinutesWithHanForTheHalf(int hour, int minute, string expected)
    {
        var formatter = new JapaneseTimeFormatter();

        var spoken = formatter.Format(new DateTime(2026, 11, 26, hour, minute, 0), new TimeFormatOptions(true, false));

        Assert.Equal(expected, spoken);
    }

    [Fact]
    public void Japanese_Reads24HourAndExactTimeInWords()
    {
        var formatter = new JapaneseTimeFormatter();

        Assert.Equal(
            "今は十九時四十五分です。",
            formatter.Format(new DateTime(2026, 11, 26, 19, 45, 0), new TimeFormatOptions(true, true)));
        Assert.Equal(
            "今は九時ちょうどです。",
            formatter.Format(new DateTime(2026, 11, 26, 9, 0, 0), new TimeFormatOptions(false, false)));
        Assert.Equal(
            "今は九時零五分です。",
            formatter.Format(new DateTime(2026, 11, 26, 9, 5, 0), new TimeFormatOptions(false, false)));
    }

    [Theory]
    [InlineData("ar")]
    [InlineData("zh-Hans")]
    [InlineData("zh-Hant")]
    [InlineData("ja-JP")]
    public void EveryMinuteOfTheDay_ProducesASentenceInAllThreeModes(string locale)
    {
        var formatter = TimeFormatterFactory.Resolve(locale).Formatter;

        foreach (var options in new[]
                 {
                     new TimeFormatOptions(true, false),
                     new TimeFormatOptions(false, false),
                     new TimeFormatOptions(true, true),
                 })
        {
            for (var minuteOfDay = 0; minuteOfDay < 24 * 60; minuteOfDay++)
            {
                var time = new DateTime(2026, 11, 26, minuteOfDay / 60, minuteOfDay % 60, 0);
                var spoken = formatter.Format(time, options);

                Assert.False(string.IsNullOrWhiteSpace(spoken));
                Assert.DoesNotContain(spoken, char.IsAsciiDigit);
            }
        }
    }
}
