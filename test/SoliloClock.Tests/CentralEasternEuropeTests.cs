using System;
using System.IO;
using System.Linq;
using SoliloClock.Services;
using Xunit;

namespace SoliloClock.Tests;

/// <summary>
/// M2.3 shares the M2.2 acceptance check: complete UI catalog, 100 phrases with
/// the reviewed metadata, its own time grammar and automatic language
/// resolution. Polish, Russian and Greek also carry plural and case agreement
/// cases, which is what makes this stage different from Western Europe.
/// </summary>
public sealed class CentralEasternEuropeTests
{
    private static string PhraseDirectory => Path.Combine(AppContext.BaseDirectory, "Phrases");

    [Theory]
    [InlineData("pl-PL", "PolishTimeFormatter")]
    [InlineData("ru-RU", "RussianTimeFormatter")]
    [InlineData("el-GR", "GreekTimeFormatter")]
    public void EachLanguage_ShipsACompletePack(string locale, string formatterType)
    {
        var ui = LocalizationCatalog.Load(LocalizationCatalog.DefaultDirectory, locale);

        Assert.False(ui.UsedFallback);
        Assert.Empty(ui.Diagnostics);
        Assert.Equal(locale, ui.Locale);

        var registered = LocaleRegistry.Find(locale);
        Assert.NotNull(registered);
        Assert.True(registered!.IsShipped);
        Assert.True(registered.HasPhraseCatalog);
        Assert.False(registered.IsProvisional);

        var selection = TimeFormatterFactory.Resolve(locale);
        Assert.Equal(formatterType, selection.Formatter.GetType().Name);
        Assert.False(selection.UsedFallback);

        var phrases = PhraseCatalog.Load(PhraseDirectory, locale);
        var english = PhraseCatalog.Load(PhraseDirectory, "en-GB");

        Assert.False(phrases.UsedFallback);
        Assert.Empty(phrases.Diagnostics);
        Assert.Equal(english.Phrases.Count, phrases.Phrases.Count);
        Assert.Equal(100, phrases.Phrases.Count);

        var englishById = english.Phrases.ToDictionary(
            phrase => phrase.Id,
            phrase => phrase.Text,
            StringComparer.OrdinalIgnoreCase);

        foreach (var phrase in phrases.Phrases)
        {
            Assert.True(englishById.ContainsKey(phrase.Id), $"{locale}: unexpected id {phrase.Id}");
            Assert.NotEqual(englishById[phrase.Id], phrase.Text);
            Assert.False(string.IsNullOrWhiteSpace(phrase.Text));
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
    [InlineData("pl", "pl-PL")]
    [InlineData("ru-BY", "ru-RU")]
    [InlineData("el-CY", "el-GR")]
    public void AutomaticLanguage_UsesTheShippedLocaleForTheSameLanguage(string system, string expected)
    {
        var choice = LocaleResolver.ResolveUi(null, system);

        Assert.Equal(expected, choice.Locale);
        Assert.False(choice.LanguageDiffers);
    }

    [Theory]
    [InlineData(9, 0, "Jest dziewiąta.")]
    [InlineData(12, 0, "Jest południe.")]
    [InlineData(0, 0, "Jest północ.")]
    [InlineData(9, 1, "Jest minuta po dziewiątej.")]
    [InlineData(9, 2, "Jest dwie minuty po dziewiątej.")]
    [InlineData(9, 5, "Jest pięć minut po dziewiątej.")]
    [InlineData(9, 21, "Jest dwadzieścia jeden minut po dziewiątej.")]
    [InlineData(9, 15, "Jest kwadrans po dziewiątej.")]
    [InlineData(9, 30, "Jest wpół do dziesiątej.")]
    [InlineData(9, 45, "Jest za kwadrans dziesiąta.")]
    [InlineData(9, 37, "Jest za dwadzieścia trzy minuty dziesiąta.")]
    [InlineData(9, 59, "Jest za minutę dziesiąta.")]
    [InlineData(23, 45, "Jest za kwadrans północ.")]
    [InlineData(1, 0, "Jest pierwsza.")]
    public void Polish_UsesFeminineHoursAndPolishPluralAgreement(
        int hour,
        int minute,
        string expected)
    {
        var formatter = new PolishTimeFormatter();

        var spoken = formatter.Format(new DateTime(2026, 11, 26, hour, minute, 0), new TimeFormatOptions(true, false));

        Assert.Equal(expected, spoken);
    }

    [Theory]
    [InlineData(19, 45, "Jest dziewiętnasta czterdzieści pięć.")]
    [InlineData(21, 0, "Jest dwudziesta pierwsza.")]
    [InlineData(0, 5, "Jest dwunasta zero pięć.")]
    public void Polish_Reads24HourTimeAsWords(int hour, int minute, string expected)
    {
        var formatter = new PolishTimeFormatter();

        var spoken = formatter.Format(new DateTime(2026, 11, 26, hour, minute, 0), new TimeFormatOptions(true, true));

        Assert.Equal(expected, spoken);
    }

    [Fact]
    public void Polish_ExactReadingSpellsMinutesTwoDigits()
    {
        var formatter = new PolishTimeFormatter();

        var spoken = formatter.Format(new DateTime(2026, 11, 26, 9, 5, 0), new TimeFormatOptions(false, false));

        Assert.Equal("Jest dziewiąta zero pięć.", spoken);
    }

    [Theory]
    [InlineData(9, 0, "Сейчас девять часов.")]
    [InlineData(1, 0, "Сейчас час.")]
    [InlineData(12, 0, "Сейчас полдень.")]
    [InlineData(0, 0, "Сейчас полночь.")]
    [InlineData(9, 2, "Сейчас две минуты десятого.")]
    [InlineData(9, 5, "Сейчас пять минут десятого.")]
    [InlineData(9, 21, "Сейчас двадцать одна минута десятого.")]
    [InlineData(9, 15, "Сейчас четверть десятого.")]
    [InlineData(9, 30, "Сейчас половина десятого.")]
    [InlineData(9, 45, "Сейчас без четверти десять.")]
    [InlineData(9, 37, "Сейчас без двадцати трёх минут десять.")]
    [InlineData(9, 59, "Сейчас без одной минуты десять.")]
    public void Russian_UsesGenitiveHoursAndRussianPluralAgreement(
        int hour,
        int minute,
        string expected)
    {
        var formatter = new RussianTimeFormatter();

        var spoken = formatter.Format(new DateTime(2026, 11, 26, hour, minute, 0), new TimeFormatOptions(true, false));

        Assert.Equal(expected, spoken);
    }

    [Theory]
    [InlineData(19, 45, "Сейчас девятнадцать часов сорок пять минут.")]
    [InlineData(21, 5, "Сейчас двадцать один час пять минут.")]
    public void Russian_Reads24HourTimeWithHourAndMinuteNouns(int hour, int minute, string expected)
    {
        var formatter = new RussianTimeFormatter();

        var spoken = formatter.Format(new DateTime(2026, 11, 26, hour, minute, 0), new TimeFormatOptions(true, true));

        Assert.Equal(expected, spoken);
    }

    [Theory]
    [InlineData(9, 0, "Είναι εννέα η ώρα.")]
    [InlineData(1, 0, "Είναι μία η ώρα.")]
    [InlineData(12, 0, "Είναι μεσημέρι.")]
    [InlineData(0, 0, "Είναι μεσάνυχτα.")]
    [InlineData(9, 1, "Είναι εννέα και ένα λεπτό.")]
    [InlineData(9, 5, "Είναι εννέα και πέντε.")]
    [InlineData(9, 15, "Είναι εννέα και τέταρτο.")]
    [InlineData(9, 30, "Είναι εννέα και μισή.")]
    [InlineData(9, 40, "Είναι δέκα παρά είκοσι.")]
    [InlineData(9, 45, "Είναι δέκα παρά τέταρτο.")]
    [InlineData(9, 59, "Είναι δέκα παρά ένα λεπτό.")]
    public void Greek_UsesFeminineHoursWithKeAndPara(int hour, int minute, string expected)
    {
        var formatter = new GreekTimeFormatter();

        var spoken = formatter.Format(new DateTime(2026, 11, 26, hour, minute, 0), new TimeFormatOptions(true, false));

        Assert.Equal(expected, spoken);
    }

    [Theory]
    [InlineData(19, 45, "Είναι δεκαεννέα και σαράντα πέντε.")]
    [InlineData(9, 0, "Είναι εννέα ακριβώς.")]
    public void Greek_Reads24HourTimeInWords(int hour, int minute, string expected)
    {
        var formatter = new GreekTimeFormatter();

        var spoken = formatter.Format(new DateTime(2026, 11, 26, hour, minute, 0), new TimeFormatOptions(true, true));

        Assert.Equal(expected, spoken);
    }

    [Theory]
    [InlineData("pl-PL")]
    [InlineData("ru-RU")]
    [InlineData("el-GR")]
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
