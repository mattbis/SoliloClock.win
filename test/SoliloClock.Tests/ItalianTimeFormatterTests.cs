using System;
using SoliloClock.Services;
using Xunit;

namespace SoliloClock.Tests;

public sealed class ItalianTimeFormatterTests
{
    private static readonly ItalianTimeFormatter Formatter = new();

    private static string Natural(int hour, int minute) =>
        Formatter.Format(
            new DateTime(2026, 11, 26, hour, minute, 0),
            new TimeFormatOptions(Natural: true, Use24Hour: false));

    private static string Exact(int hour, int minute, bool use24Hour) =>
        Formatter.Format(
            new DateTime(2026, 11, 26, hour, minute, 0),
            new TimeFormatOptions(Natural: false, Use24Hour: use24Hour));

    [Theory]
    [InlineData(9, 0, "Sono le nove.")]
    [InlineData(9, 15, "Sono le nove e un quarto.")]
    [InlineData(9, 30, "Sono le nove e mezza.")]
    [InlineData(9, 45, "Sono le dieci meno un quarto.")]
    [InlineData(9, 5, "Sono le nove e cinque.")]
    [InlineData(9, 20, "Sono le nove e venti.")]
    [InlineData(9, 40, "Sono le dieci meno venti.")]
    [InlineData(9, 59, "Sono le dieci meno uno.")]
    [InlineData(1, 0, "È l'una.")]
    [InlineData(1, 15, "È l'una e un quarto.")]
    [InlineData(0, 0, "È mezzanotte.")]
    [InlineData(12, 0, "È mezzogiorno.")]
    [InlineData(12, 30, "Sono le dodici e mezza.")]
    [InlineData(12, 45, "È l'una meno un quarto.")]
    [InlineData(0, 30, "Sono le dodici e mezza.")]
    [InlineData(23, 45, "Sono le dodici meno un quarto.")]
    [InlineData(19, 45, "Sono le otto meno un quarto.")]
    [InlineData(23, 59, "Sono le dodici meno uno.")]
    public void NaturalTwelveHour_UsesItalianClockPhrasing(int hour, int minute, string expected) =>
        Assert.Equal(expected, Natural(hour, minute));

    [Theory]
    [InlineData(19, 45, "Sono le diciannove e quarantacinque.")]
    [InlineData(21, 0, "Sono le ventuno.")]
    [InlineData(0, 0, "È mezzanotte.")]
    [InlineData(0, 5, "Sono le zero e cinque.")]
    [InlineData(1, 5, "È l'una e cinque.")]
    [InlineData(12, 0, "Sono le dodici.")]
    [InlineData(23, 59, "Sono le ventitré e cinquantanove.")]
    public void TwentyFourHour_ReadsTheLiteralTwentyFourHourTime(int hour, int minute, string expected) =>
        Assert.Equal(expected, Exact(hour, minute, use24Hour: true));

    [Theory]
    [InlineData(19, 45, "Sono le sette e quarantacinque.")]
    [InlineData(12, 5, "Sono le dodici e cinque.")]
    [InlineData(0, 5, "Sono le dodici e cinque.")]
    [InlineData(9, 30, "Sono le nove e trenta.")]
    [InlineData(1, 0, "È l'una.")]
    public void ExactTwelveHour_SpellsEveryNumber(int hour, int minute, string expected) =>
        Assert.Equal(expected, Exact(hour, minute, use24Hour: false));

    [Fact]
    public void ItalianNumbers_ElideBeforeUnoAndOttoAndAccentTre()
    {
        // 21 "ventuno", 23 "ventitré", 28 "ventotto", 31 "trentuno".
        Assert.Equal("Sono le ventuno.", Exact(21, 0, use24Hour: true));
        Assert.Equal("Sono le ventitré.", Exact(23, 0, use24Hour: true));
        Assert.Equal("Sono le nove e ventuno.", Natural(9, 21));
        Assert.Equal("Sono le nove e ventitré.", Natural(9, 23));
        Assert.Equal("Sono le nove e ventotto.", Natural(9, 28));
        Assert.Equal("Sono le dieci e trentuno.", Exact(10, 31, use24Hour: true));
    }

    [Fact]
    public void EveryMinute_ProducesAnItalianSentence()
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

                    Assert.True(
                        phrase.StartsWith("È ", StringComparison.Ordinal) ||
                        phrase.StartsWith("Sono le ", StringComparison.Ordinal),
                        phrase);
                    Assert.EndsWith(".", phrase, StringComparison.Ordinal);
                    Assert.DoesNotContain("o'clock", phrase);
                    Assert.DoesNotContain("  ", phrase);
                }
            }
        }
    }
}
