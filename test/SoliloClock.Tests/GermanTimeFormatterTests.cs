using System;
using SoliloClock.Services;
using Xunit;

namespace SoliloClock.Tests;

public sealed class GermanTimeFormatterTests
{
    private static readonly GermanTimeFormatter Formatter = new();

    private static string Natural(int hour, int minute) =>
        Formatter.Format(
            new DateTime(2026, 11, 26, hour, minute, 0),
            new TimeFormatOptions(Natural: true, Use24Hour: false));

    private static string Exact(int hour, int minute, bool use24Hour) =>
        Formatter.Format(
            new DateTime(2026, 11, 26, hour, minute, 0),
            new TimeFormatOptions(Natural: false, Use24Hour: use24Hour));

    [Theory]
    [InlineData(9, 0, "Es ist neun Uhr.")]
    [InlineData(9, 15, "Es ist Viertel nach neun.")]
    [InlineData(9, 30, "Es ist halb zehn.")]
    [InlineData(9, 45, "Es ist Viertel vor zehn.")]
    [InlineData(9, 5, "Es ist neun Uhr fünf.")]
    [InlineData(9, 1, "Es ist neun Uhr eins.")]
    [InlineData(9, 20, "Es ist neun Uhr zwanzig.")]
    [InlineData(9, 40, "Es ist neun Uhr vierzig.")]
    [InlineData(9, 59, "Es ist neun Uhr neunundfünfzig.")]
    [InlineData(1, 0, "Es ist ein Uhr.")]
    [InlineData(0, 0, "Es ist Mitternacht.")]
    [InlineData(12, 0, "Es ist Mittag.")]
    [InlineData(12, 30, "Es ist halb eins.")]
    [InlineData(0, 15, "Es ist Viertel nach zwölf.")]
    [InlineData(0, 30, "Es ist halb eins.")]
    [InlineData(12, 45, "Es ist Viertel vor eins.")]
    [InlineData(0, 45, "Es ist Viertel vor eins.")]
    [InlineData(1, 45, "Es ist Viertel vor zwei.")]
    [InlineData(11, 45, "Es ist Viertel vor zwölf.")]
    [InlineData(23, 45, "Es ist Viertel vor zwölf.")]
    [InlineData(19, 30, "Es ist halb acht.")]
    [InlineData(19, 45, "Es ist Viertel vor acht.")]
    [InlineData(23, 59, "Es ist elf Uhr neunundfünfzig.")]
    public void NaturalTwelveHour_UsesGermanClockPhrasing(int hour, int minute, string expected) =>
        Assert.Equal(expected, Natural(hour, minute));

    [Theory]
    [InlineData(19, 45, "Es ist neunzehn Uhr fünfundvierzig.")]
    [InlineData(21, 0, "Es ist einundzwanzig Uhr.")]
    [InlineData(0, 0, "Es ist null Uhr.")]
    [InlineData(0, 5, "Es ist null Uhr fünf.")]
    [InlineData(1, 5, "Es ist ein Uhr fünf.")]
    [InlineData(12, 0, "Es ist zwölf Uhr.")]
    [InlineData(23, 59, "Es ist dreiundzwanzig Uhr neunundfünfzig.")]
    public void TwentyFourHour_ReadsTheLiteralTwentyFourHourTime(int hour, int minute, string expected) =>
        Assert.Equal(expected, Exact(hour, minute, use24Hour: true));

    [Theory]
    [InlineData(19, 45, "Es ist sieben Uhr fünfundvierzig.")]
    [InlineData(12, 5, "Es ist zwölf Uhr fünf.")]
    [InlineData(0, 5, "Es ist zwölf Uhr fünf.")]
    [InlineData(9, 30, "Es ist neun Uhr dreißig.")]
    [InlineData(1, 0, "Es ist ein Uhr.")]
    public void ExactTwelveHour_SpellsEveryNumber(int hour, int minute, string expected) =>
        Assert.Equal(expected, Exact(hour, minute, use24Hour: false));

    [Fact]
    public void One_UsesEinWithUhrAndEinsAfterHalbOrViertel()
    {
        // "ein Uhr", never "eins Uhr"; but "halb eins", "Viertel vor eins".
        Assert.Equal("Es ist ein Uhr fünf.", Exact(1, 5, use24Hour: true));
        Assert.Equal("Es ist halb eins.", Natural(0, 30));
        Assert.Equal("Es ist Viertel vor eins.", Natural(12, 45));
        Assert.Equal("Es ist einundzwanzig Uhr.", Exact(21, 0, use24Hour: true));
        Assert.Equal("Es ist einundzwanzig Uhr einundzwanzig.", Exact(21, 21, use24Hour: true));
    }

    [Fact]
    public void EveryMinute_ProducesAGermanSentence()
    {
        for (var hour = 0; hour < 24; hour++)
        {
            for (var minute = 0; minute < 60; minute++)
            {
                foreach (var options in new[]
                {
                    new TimeFormatOptions(Natural: true, Use24Hour: false),
                    new TimeFormatOptions(Natural: false, Use24Hour: false),
                    new TimeFormatOptions(Natural: true, Use24Hour: true),
                })
                {
                    var phrase = Formatter.Format(new DateTime(2026, 11, 26, hour, minute, 0), options);

                    Assert.StartsWith("Es ist ", phrase, StringComparison.Ordinal);
                    Assert.EndsWith(".", phrase, StringComparison.Ordinal);
                    Assert.DoesNotContain("o'clock", phrase);
                    Assert.DoesNotContain("past", phrase);
                    Assert.DoesNotContain("  ", phrase);
                }
            }
        }
    }
}
