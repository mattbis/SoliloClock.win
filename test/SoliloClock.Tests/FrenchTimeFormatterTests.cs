using System;
using SoliloClock.Services;
using Xunit;

namespace SoliloClock.Tests;

public sealed class FrenchTimeFormatterTests
{
    private static readonly FrenchTimeFormatter Formatter = new();

    private static string Natural(int hour, int minute) =>
        Formatter.Format(
            new DateTime(2026, 11, 26, hour, minute, 0),
            new TimeFormatOptions(Natural: true, Use24Hour: false));

    private static string Exact(int hour, int minute, bool use24Hour) =>
        Formatter.Format(
            new DateTime(2026, 11, 26, hour, minute, 0),
            new TimeFormatOptions(Natural: false, Use24Hour: use24Hour));

    [Theory]
    [InlineData(9, 0, "Il est neuf heures.")]
    [InlineData(9, 15, "Il est neuf heures et quart.")]
    [InlineData(9, 30, "Il est neuf heures et demie.")]
    [InlineData(9, 45, "Il est dix heures moins le quart.")]
    [InlineData(9, 20, "Il est neuf heures vingt.")]
    [InlineData(9, 40, "Il est dix heures moins vingt.")]
    [InlineData(9, 1, "Il est neuf heures une.")]
    [InlineData(11, 45, "Il est midi moins le quart.")]
    [InlineData(12, 0, "Il est midi.")]
    [InlineData(12, 30, "Il est midi et demi.")]
    [InlineData(0, 0, "Il est minuit.")]
    [InlineData(0, 15, "Il est minuit et quart.")]
    [InlineData(1, 0, "Il est une heure du matin.")]
    [InlineData(4, 45, "Il est cinq heures moins le quart du matin.")]
    [InlineData(14, 5, "Il est deux heures cinq de l’après-midi.")]
    [InlineData(19, 45, "Il est huit heures moins le quart du soir.")]
    [InlineData(21, 0, "Il est neuf heures du soir.")]
    [InlineData(23, 45, "Il est minuit moins le quart.")]
    public void NaturalTwelveHour_UsesFrenchClockPhrasing(int hour, int minute, string expected) =>
        Assert.Equal(expected, Natural(hour, minute));

    [Theory]
    [InlineData(19, 45, "Il est dix-neuf heures quarante-cinq.")]
    [InlineData(21, 0, "Il est vingt et une heures.")]
    [InlineData(19, 1, "Il est dix-neuf heures une.")]
    [InlineData(1, 5, "Il est une heure cinq.")]
    [InlineData(0, 0, "Il est minuit.")]
    [InlineData(0, 1, "Il est minuit et une.")]
    [InlineData(12, 0, "Il est midi.")]
    public void TwentyFourHour_ReadsExactTimeWithoutADayPart(int hour, int minute, string expected) =>
        Assert.Equal(expected, Exact(hour, minute, use24Hour: true));

    [Theory]
    [InlineData(19, 45, "Il est sept heures quarante-cinq.")]
    [InlineData(12, 5, "Il est midi cinq.")]
    [InlineData(9, 1, "Il est neuf heures une.")]
    [InlineData(0, 0, "Il est minuit.")]
    public void ExactTwelveHour_SpellsEveryNumber(int hour, int minute, string expected) =>
        Assert.Equal(expected, Exact(hour, minute, use24Hour: false));

    [Fact]
    public void FrenchPhrasing_DoesNotFallBackToEnglishWords()
    {
        foreach (var minute in new[] { 0, 15, 30, 45, 20, 40 })
        {
            var phrase = Natural(7, minute);

            Assert.StartsWith("Il est ", phrase);
            Assert.DoesNotContain("o'clock", phrase);
            Assert.DoesNotContain("past", phrase);
            Assert.DoesNotContain("quarter to", phrase);
        }
    }

    [Fact]
    public void HoursAndMinutes_AgreeInTheFeminine()
    {
        // 21:00 and 21 minutes past the hour both use "vingt et une".
        Assert.Equal("Il est vingt et une heures.", Exact(21, 0, use24Hour: true));
        Assert.Equal("Il est neuf heures vingt et une.", Natural(9, 21));
        Assert.Equal("Il est dix heures moins vingt et une.", Natural(9, 39));
    }
}
