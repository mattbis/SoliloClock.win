using System;
using System.IO;
using System.Linq;
using SoliloClock.Services;
using Xunit;

namespace SoliloClock.Tests;

/// <summary>
/// Completes M2.5 and M2.6: Hindi and Korean ship complete packs. Both are
/// machine-drafted, so both carry the provisional flag until a native speaker
/// reviews the wording.
/// </summary>
public sealed class SouthAsiaAndKoreaTests
{
    private static string PhraseDirectory => Path.Combine(AppContext.BaseDirectory, "Phrases");

    [Theory]
    [InlineData("hi", "HindiTimeFormatter")]
    [InlineData("ko-KR", "KoreanTimeFormatter")]
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
    [InlineData("hi-IN", "hi")]
    [InlineData("ko-KR", "ko-KR")]
    public void AutomaticLanguage_FindsTheShippedLocale(string system, string expected)
    {
        var choice = LocaleResolver.ResolveUi(null, system);

        Assert.Equal(expected, choice.Locale);
        Assert.False(choice.LanguageDiffers);
    }

    [Theory]
    [InlineData(9, 0, "अभी नौ बजे हैं।")]
    [InlineData(12, 0, "अभी दोपहर है।")]
    [InlineData(0, 0, "अभी आधी रात है।")]
    [InlineData(9, 5, "अभी नौ बजकर पाँच मिनट हैं।")]
    [InlineData(9, 15, "अभी सवा नौ बजे हैं।")]
    [InlineData(9, 30, "अभी साढ़े नौ बजे हैं।")]
    [InlineData(9, 40, "अभी नौ बजकर चालीस मिनट हैं।")]
    [InlineData(9, 45, "अभी पौने दस बजे हैं।")]
    [InlineData(9, 50, "अभी दस बजने में दस मिनट हैं।")]
    public void Hindi_UsesSawaSadeAndPauneForTheQuarters(int hour, int minute, string expected)
    {
        var formatter = new HindiTimeFormatter();

        var spoken = formatter.Format(new DateTime(2026, 11, 26, hour, minute, 0), new TimeFormatOptions(true, false));

        Assert.Equal(expected, spoken);
    }

    [Fact]
    public void Hindi_Reads24HourAndExactTimeInWords()
    {
        var formatter = new HindiTimeFormatter();

        Assert.Equal(
            "अभी उन्नीस बजकर पैंतालीस मिनट हैं।",
            formatter.Format(new DateTime(2026, 11, 26, 19, 45, 0), new TimeFormatOptions(true, true)));
        Assert.Equal(
            "अभी इक्कीस बजे हैं।",
            formatter.Format(new DateTime(2026, 11, 26, 21, 0, 0), new TimeFormatOptions(true, true)));
        Assert.Equal(
            "अभी नौ बजकर शून्य पाँच मिनट हैं।",
            formatter.Format(new DateTime(2026, 11, 26, 9, 5, 0), new TimeFormatOptions(false, false)));
    }

    [Theory]
    [InlineData(1, 0, "지금은 한 시입니다.")]
    [InlineData(9, 0, "지금은 아홉 시입니다.")]
    [InlineData(12, 0, "지금은 정오입니다.")]
    [InlineData(0, 0, "지금은 자정입니다.")]
    [InlineData(9, 5, "지금은 아홉 시 오 분입니다.")]
    [InlineData(9, 15, "지금은 아홉 시 십오 분입니다.")]
    [InlineData(9, 30, "지금은 아홉 시 반입니다.")]
    [InlineData(9, 45, "지금은 아홉 시 사십오 분입니다.")]
    public void Korean_UsesNativeHoursWithSinoMinutes(int hour, int minute, string expected)
    {
        var formatter = new KoreanTimeFormatter();

        var spoken = formatter.Format(new DateTime(2026, 11, 26, hour, minute, 0), new TimeFormatOptions(true, false));

        Assert.Equal(expected, spoken);
    }

    [Fact]
    public void Korean_Reads24HourAndExactTimeInWords()
    {
        var formatter = new KoreanTimeFormatter();

        Assert.Equal(
            "지금은 십구 시 사십오 분입니다.",
            formatter.Format(new DateTime(2026, 11, 26, 19, 45, 0), new TimeFormatOptions(true, true)));
        Assert.Equal(
            "지금은 아홉 시 정각입니다.",
            formatter.Format(new DateTime(2026, 11, 26, 9, 0, 0), new TimeFormatOptions(false, false)));
        Assert.Equal(
            "지금은 아홉 시 영오 분입니다.",
            formatter.Format(new DateTime(2026, 11, 26, 9, 5, 0), new TimeFormatOptions(false, false)));
    }

    [Theory]
    [InlineData("hi")]
    [InlineData("ko-KR")]
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
