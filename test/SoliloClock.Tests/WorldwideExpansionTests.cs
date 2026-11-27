using System;
using System.IO;
using System.Linq;
using SoliloClock.Services;
using Xunit;

namespace SoliloClock.Tests;

/// <summary>
/// M2.7 closes the gap against the Windows text-to-speech list. This first wave
/// is the Nordic block plus Czech: all four are machine-drafted, so each is
/// provisional while still shipping a complete catalog, phrase pack and grammar.
/// </summary>
public sealed class WorldwideExpansionTests
{
    private static string PhraseDirectory => Path.Combine(AppContext.BaseDirectory, "Phrases");

    [Theory]
    [InlineData("da-DK", "DanishTimeFormatter")]
    [InlineData("sv-SE", "SwedishTimeFormatter")]
    [InlineData("nb-NO", "NorwegianTimeFormatter")]
    [InlineData("cs-CZ", "CzechTimeFormatter")]
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
    [InlineData("da", "da-DK")]
    [InlineData("sv-FI", "sv-SE")]
    [InlineData("nb", "nb-NO")]
    [InlineData("cs-SK", "cs-CZ")]
    public void AutomaticLanguage_UsesTheShippedLocaleForTheSameLanguage(string system, string expected)
    {
        var choice = LocaleResolver.ResolveUi(null, system);

        Assert.Equal(expected, choice.Locale);
        Assert.False(choice.LanguageDiffers);
    }

    [Theory]
    [InlineData(9, 0, "Klokken er ni.")]
    [InlineData(12, 0, "Klokken er middag.")]
    [InlineData(0, 0, "Klokken er midnat.")]
    [InlineData(9, 1, "Klokken er et minut over ni.")]
    [InlineData(9, 5, "Klokken er fem minutter over ni.")]
    [InlineData(9, 15, "Klokken er kvart over ni.")]
    [InlineData(9, 30, "Klokken er halv ti.")]
    [InlineData(9, 40, "Klokken er tyve minutter i ti.")]
    [InlineData(9, 45, "Klokken er kvart i ti.")]
    [InlineData(9, 50, "Klokken er ti minutter i ti.")]
    public void Danish_CountsTowardsTheNextHourForTheHalf(int hour, int minute, string expected)
    {
        var formatter = new DanishTimeFormatter();

        Assert.Equal(expected, formatter.Format(
            new DateTime(2026, 11, 26, hour, minute, 0),
            new TimeFormatOptions(true, false)));
    }

    [Fact]
    public void Danish_Reads24HourAndExactTimeInWords()
    {
        var formatter = new DanishTimeFormatter();

        Assert.Equal(
            "Klokken er nitten femogfyrre.",
            formatter.Format(new DateTime(2026, 11, 26, 19, 45, 0), new TimeFormatOptions(true, true)));
        Assert.Equal(
            "Klokken er ni nul fem.",
            formatter.Format(new DateTime(2026, 11, 26, 9, 5, 0), new TimeFormatOptions(false, false)));
    }

    [Theory]
    [InlineData(9, 0, "Klockan är nio.")]
    [InlineData(12, 0, "Klockan är mitt på dagen.")]
    [InlineData(0, 0, "Klockan är midnatt.")]
    [InlineData(9, 1, "Klockan är en minut över nio.")]
    [InlineData(9, 5, "Klockan är fem över nio.")]
    [InlineData(9, 15, "Klockan är kvart över nio.")]
    [InlineData(9, 30, "Klockan är halv tio.")]
    [InlineData(9, 40, "Klockan är tjugo i tio.")]
    [InlineData(9, 45, "Klockan är kvart i tio.")]
    public void Swedish_UsesOverAndIHalfToTheNextHour(int hour, int minute, string expected)
    {
        var formatter = new SwedishTimeFormatter();

        Assert.Equal(expected, formatter.Format(
            new DateTime(2026, 11, 26, hour, minute, 0),
            new TimeFormatOptions(true, false)));
    }

    [Fact]
    public void Swedish_Reads24HourAndExactTimeInWords()
    {
        var formatter = new SwedishTimeFormatter();

        Assert.Equal(
            "Klockan är nitton fyrtiofem.",
            formatter.Format(new DateTime(2026, 11, 26, 19, 45, 0), new TimeFormatOptions(true, true)));
        Assert.Equal(
            "Klockan är nio noll fem.",
            formatter.Format(new DateTime(2026, 11, 26, 9, 5, 0), new TimeFormatOptions(false, false)));
    }

    [Theory]
    [InlineData(9, 0, "Klokka er ni.")]
    [InlineData(12, 0, "Klokka er midt på dagen.")]
    [InlineData(0, 0, "Klokka er midnatt.")]
    [InlineData(9, 1, "Klokka er ett minutt over ni.")]
    [InlineData(9, 5, "Klokka er fem over ni.")]
    [InlineData(9, 15, "Klokka er kvart over ni.")]
    [InlineData(9, 30, "Klokka er halv ti.")]
    [InlineData(9, 40, "Klokka er tjue på ti.")]
    [InlineData(9, 45, "Klokka er kvart på ti.")]
    public void Norwegian_UsesPaWhereDanishAndSwedishUseI(int hour, int minute, string expected)
    {
        var formatter = new NorwegianTimeFormatter();

        Assert.Equal(expected, formatter.Format(
            new DateTime(2026, 11, 26, hour, minute, 0),
            new TimeFormatOptions(true, false)));
    }

    [Fact]
    public void Norwegian_Reads24HourAndExactTimeInWords()
    {
        var formatter = new NorwegianTimeFormatter();

        Assert.Equal(
            "Klokka er nitten førtifem.",
            formatter.Format(new DateTime(2026, 11, 26, 19, 45, 0), new TimeFormatOptions(true, true)));
        Assert.Equal(
            "Klokka er ni null fem.",
            formatter.Format(new DateTime(2026, 11, 26, 9, 5, 0), new TimeFormatOptions(false, false)));
    }

    [Theory]
    [InlineData(9, 0, "Je devět hodin.")]
    [InlineData(1, 0, "Je jedna hodina.")]
    [InlineData(2, 0, "Jsou dvě hodiny.")]
    [InlineData(0, 0, "Je půlnoc.")]
    [InlineData(12, 0, "Je poledne.")]
    [InlineData(9, 1, "Je jedna minuta po deváté.")]
    [InlineData(9, 5, "Je pět minut po deváté.")]
    [InlineData(9, 15, "Je čtvrt na deset.")]
    [InlineData(9, 30, "Je půl desáté.")]
    [InlineData(9, 40, "Je za dvacet minut deset.")]
    [InlineData(9, 45, "Je tři čtvrtě na deset.")]
    [InlineData(9, 50, "Je za deset minut deset.")]
    [InlineData(9, 59, "Je za jednu minutu deset.")]
    public void Czech_CountsTowardsTheNextHourWithCaseAgreement(int hour, int minute, string expected)
    {
        var formatter = new CzechTimeFormatter();

        Assert.Equal(expected, formatter.Format(
            new DateTime(2026, 11, 26, hour, minute, 0),
            new TimeFormatOptions(true, false)));
    }

    [Fact]
    public void Czech_Reads24HourAndExactTimeInWords()
    {
        var formatter = new CzechTimeFormatter();

        Assert.Equal(
            "Je devatenáct hodin čtyřicet pět minut.",
            formatter.Format(new DateTime(2026, 11, 26, 19, 45, 0), new TimeFormatOptions(true, true)));
        Assert.Equal(
            "Je devět hodin pět minut.",
            formatter.Format(new DateTime(2026, 11, 26, 9, 5, 0), new TimeFormatOptions(false, false)));
    }

    [Theory]
    [InlineData("da-DK")]
    [InlineData("sv-SE")]
    [InlineData("nb-NO")]
    [InlineData("cs-CZ")]
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
