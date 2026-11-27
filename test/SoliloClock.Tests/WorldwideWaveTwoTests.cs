using System;
using System.IO;
using System.Linq;
using SoliloClock.Services;
using Xunit;

namespace SoliloClock.Tests;

/// <summary>
/// M2.7 wave 2: Turkish, Hungarian, Romanian, Croatian, Bulgarian, Slovak and
/// Finnish. All are machine-drafted, so each ships provisionally with a complete
/// catalog, phrase pack and grammar.
/// </summary>
public sealed class WorldwideWaveTwoTests
{
    private static string PhraseDirectory => Path.Combine(AppContext.BaseDirectory, "Phrases");

    [Theory]
    [InlineData("tr-TR", "TurkishTimeFormatter")]
    [InlineData("hu-HU", "HungarianTimeFormatter")]
    [InlineData("ro-RO", "RomanianTimeFormatter")]
    [InlineData("hr-HR", "CroatianTimeFormatter")]
    [InlineData("bg-BG", "BulgarianTimeFormatter")]
    [InlineData("sk-SK", "SlovakTimeFormatter")]
    [InlineData("fi-FI", "FinnishTimeFormatter")]
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
    [InlineData("tr", "tr-TR")]
    [InlineData("hu", "hu-HU")]
    [InlineData("ro", "ro-RO")]
    [InlineData("hr", "hr-HR")]
    [InlineData("bg", "bg-BG")]
    [InlineData("sk", "sk-SK")]
    [InlineData("fi", "fi-FI")]
    public void AutomaticLanguage_UsesTheShippedLocaleForTheSameLanguage(string system, string expected)
    {
        var choice = LocaleResolver.ResolveUi(null, system);

        Assert.Equal(expected, choice.Locale);
        Assert.False(choice.LanguageDiffers);
    }

    [Theory]
    [InlineData(9, 0, "Saat dokuz.")]
    [InlineData(12, 0, "Saat on iki.")]
    [InlineData(0, 0, "Gece yarısı.")]
    [InlineData(9, 1, "Saat dokuzu bir geçiyor.")]
    [InlineData(9, 5, "Saat dokuzu beş geçiyor.")]
    [InlineData(9, 15, "Saat dokuzu çeyrek geçiyor.")]
    [InlineData(9, 30, "Saat dokuz buçuk.")]
    [InlineData(9, 40, "Saat ona yirmi var.")]
    [InlineData(9, 45, "Saat ona çeyrek var.")]
    public void Turkish_UsesAccusativePastAndDativeTo(int hour, int minute, string expected)
    {
        var formatter = new TurkishTimeFormatter();

        Assert.Equal(expected, formatter.Format(
            new DateTime(2026, 11, 26, hour, minute, 0),
            new TimeFormatOptions(true, false)));
    }

    [Fact]
    public void Turkish_Reads24HourAndExactTimeInWords()
    {
        var formatter = new TurkishTimeFormatter();

        Assert.Equal(
            "Saat on dokuz kırk beş.",
            formatter.Format(new DateTime(2026, 11, 26, 19, 45, 0), new TimeFormatOptions(true, true)));
        Assert.Equal(
            "Saat dokuz sıfır beş.",
            formatter.Format(new DateTime(2026, 11, 26, 9, 5, 0), new TimeFormatOptions(false, false)));
    }

    [Theory]
    [InlineData(9, 0, "Kilenc óra van.")]
    [InlineData(12, 0, "Dél van.")]
    [InlineData(0, 0, "Éjfél van.")]
    [InlineData(9, 5, "Öt perccel múlt kilenc.")]
    [InlineData(9, 15, "Negyed tíz van.")]
    [InlineData(9, 30, "Fél tíz van.")]
    [InlineData(9, 40, "Húsz perc múlva tíz óra.")]
    [InlineData(9, 45, "Háromnegyed tíz van.")]
    public void Hungarian_NamesTheComingHourForQuarters(int hour, int minute, string expected)
    {
        var formatter = new HungarianTimeFormatter();

        Assert.Equal(expected, formatter.Format(
            new DateTime(2026, 11, 26, hour, minute, 0),
            new TimeFormatOptions(true, false)));
    }

    [Fact]
    public void Hungarian_Reads24HourAndExactTimeInWords()
    {
        var formatter = new HungarianTimeFormatter();

        Assert.Equal(
            "Tizenkilenc óra negyvenöt perc.",
            formatter.Format(new DateTime(2026, 11, 26, 19, 45, 0), new TimeFormatOptions(true, true)));
        Assert.Equal(
            "Kilenc óra öt perc.",
            formatter.Format(new DateTime(2026, 11, 26, 9, 5, 0), new TimeFormatOptions(false, false)));
    }

    [Theory]
    [InlineData(9, 0, "Este ora nouă.")]
    [InlineData(12, 0, "Este amiază.")]
    [InlineData(0, 0, "Este miezul nopții.")]
    [InlineData(9, 1, "Este ora nouă și un minut.")]
    [InlineData(9, 5, "Este ora nouă și cinci minute.")]
    [InlineData(9, 15, "Este ora nouă și un sfert.")]
    [InlineData(9, 30, "Este ora nouă și jumătate.")]
    [InlineData(9, 40, "Este ora zece fără douăzeci de minute.")]
    [InlineData(9, 45, "Este ora zece fără un sfert.")]
    public void Romanian_UsesFaraAndDeBeforeTwenty(int hour, int minute, string expected)
    {
        var formatter = new RomanianTimeFormatter();

        Assert.Equal(expected, formatter.Format(
            new DateTime(2026, 11, 26, hour, minute, 0),
            new TimeFormatOptions(true, false)));
    }

    [Fact]
    public void Romanian_Reads24HourInWords()
    {
        var formatter = new RomanianTimeFormatter();

        Assert.Equal(
            "Este ora nouăsprezece și patruzeci și cinci de minute.",
            formatter.Format(new DateTime(2026, 11, 26, 19, 45, 0), new TimeFormatOptions(true, true)));
        Assert.Equal(
            "Este ora nouă și cinci minute.",
            formatter.Format(new DateTime(2026, 11, 26, 9, 5, 0), new TimeFormatOptions(false, false)));
    }

    [Theory]
    [InlineData(9, 0, "Sada je devet sati.")]
    [InlineData(1, 0, "Sada je jedan sat.")]
    [InlineData(12, 0, "Sada je podne.")]
    [InlineData(0, 0, "Sada je ponoć.")]
    [InlineData(9, 5, "Sada je devet sati i pet minuta.")]
    [InlineData(9, 15, "Sada je devet sati i petnaest minuta.")]
    [InlineData(9, 30, "Sada je devet sati i trideset minuta.")]
    [InlineData(9, 40, "Sada je dvadeset minuta do deset.")]
    [InlineData(9, 45, "Sada je petnaest minuta do deset.")]
    public void Croatian_AgreesTheHourNounAndCountsToTheNextHour(int hour, int minute, string expected)
    {
        var formatter = new CroatianTimeFormatter();

        Assert.Equal(expected, formatter.Format(
            new DateTime(2026, 11, 26, hour, minute, 0),
            new TimeFormatOptions(true, false)));
    }

    [Fact]
    public void Croatian_Reads24HourInWords()
    {
        var formatter = new CroatianTimeFormatter();

        Assert.Equal(
            "Sada je devetnaest sati i četrdeset pet minuta.",
            formatter.Format(new DateTime(2026, 11, 26, 19, 45, 0), new TimeFormatOptions(true, true)));
    }

    [Theory]
    [InlineData(9, 0, "Сега е девет часът.")]
    [InlineData(12, 0, "Сега е обед.")]
    [InlineData(0, 0, "Сега е полунощ.")]
    [InlineData(9, 5, "Сега е девет и пет минути.")]
    [InlineData(9, 15, "Сега е девет и петнадесет.")]
    [InlineData(9, 30, "Сега е девет и половина.")]
    [InlineData(9, 40, "Сега е без двадесет минути десет.")]
    [InlineData(9, 45, "Сега е без петнадесет десет.")]
    public void Bulgarian_UsesBezForMinutesToTheHour(int hour, int minute, string expected)
    {
        var formatter = new BulgarianTimeFormatter();

        Assert.Equal(expected, formatter.Format(
            new DateTime(2026, 11, 26, hour, minute, 0),
            new TimeFormatOptions(true, false)));
    }

    [Fact]
    public void Bulgarian_Reads24HourInWords()
    {
        var formatter = new BulgarianTimeFormatter();

        Assert.Equal(
            "Сега е деветнадесет часа и четиридесет и пет минути.",
            formatter.Format(new DateTime(2026, 11, 26, 19, 45, 0), new TimeFormatOptions(true, true)));
        Assert.Equal(
            "Сега е девет часът и пет минути.",
            formatter.Format(new DateTime(2026, 11, 26, 9, 5, 0), new TimeFormatOptions(false, false)));
    }

    [Theory]
    [InlineData(9, 0, "Je deväť hodín.")]
    [InlineData(1, 0, "Je jedna hodina.")]
    [InlineData(2, 0, "Sú dve hodiny.")]
    [InlineData(0, 0, "Je polnoc.")]
    [InlineData(12, 0, "Je poludnie.")]
    [InlineData(9, 5, "Je päť minút po deviatej.")]
    [InlineData(9, 15, "Je štvrť na desať.")]
    [InlineData(9, 30, "Je pol desiatej.")]
    [InlineData(9, 40, "Je za dvadsať minút desať.")]
    [InlineData(9, 45, "Je tri štvrte na desať.")]
    [InlineData(9, 59, "Je za jednu minútu desať.")]
    public void Slovak_CountsTowardsTheNextHourWithCaseAgreement(int hour, int minute, string expected)
    {
        var formatter = new SlovakTimeFormatter();

        Assert.Equal(expected, formatter.Format(
            new DateTime(2026, 11, 26, hour, minute, 0),
            new TimeFormatOptions(true, false)));
    }

    [Fact]
    public void Slovak_Reads24HourAndExactTimeInWords()
    {
        var formatter = new SlovakTimeFormatter();

        Assert.Equal(
            "Je devätnásť hodín štyridsaťpäť minút.",
            formatter.Format(new DateTime(2026, 11, 26, 19, 45, 0), new TimeFormatOptions(true, true)));
        Assert.Equal(
            "Je deväť hodín päť minút.",
            formatter.Format(new DateTime(2026, 11, 26, 9, 5, 0), new TimeFormatOptions(false, false)));
    }

    [Theory]
    [InlineData(9, 0, "Kello on yhdeksän.")]
    [InlineData(12, 0, "Kello on keskipäivä.")]
    [InlineData(0, 0, "Kello on keskiyö.")]
    [InlineData(9, 1, "Kello on minuutti yli yhdeksän.")]
    [InlineData(9, 5, "Kello on viisi yli yhdeksän.")]
    [InlineData(9, 15, "Kello on varttia yli yhdeksän.")]
    [InlineData(9, 30, "Kello on puoli kymmenen.")]
    [InlineData(9, 40, "Kello on kaksikymmentä vaille kymmenen.")]
    [InlineData(9, 45, "Kello on varttia vaille kymmenen.")]
    public void Finnish_UsesGenitiveForPuoliAndVaille(int hour, int minute, string expected)
    {
        var formatter = new FinnishTimeFormatter();

        Assert.Equal(expected, formatter.Format(
            new DateTime(2026, 11, 26, hour, minute, 0),
            new TimeFormatOptions(true, false)));
    }

    [Fact]
    public void Finnish_Reads24HourAndExactTimeInWords()
    {
        var formatter = new FinnishTimeFormatter();

        Assert.Equal(
            "Kello on yhdeksäntoista neljäkymmentäviisi.",
            formatter.Format(new DateTime(2026, 11, 26, 19, 45, 0), new TimeFormatOptions(true, true)));
        Assert.Equal(
            "Kello on yhdeksän viisi minuuttia.",
            formatter.Format(new DateTime(2026, 11, 26, 9, 5, 0), new TimeFormatOptions(false, false)));
    }

    [Theory]
    [InlineData("tr-TR")]
    [InlineData("hu-HU")]
    [InlineData("ro-RO")]
    [InlineData("hr-HR")]
    [InlineData("bg-BG")]
    [InlineData("sk-SK")]
    [InlineData("fi-FI")]
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
