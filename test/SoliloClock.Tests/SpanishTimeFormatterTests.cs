using System;
using SoliloClock.Services;
using Xunit;

namespace SoliloClock.Tests;

public sealed class SpanishTimeFormatterTests
{
    private static readonly SpanishTimeFormatter Formatter = new();

    private static string Natural(int hour, int minute) =>
        Formatter.Format(
            new DateTime(2026, 11, 26, hour, minute, 0),
            new TimeFormatOptions(Natural: true, Use24Hour: false));

    private static string Exact(int hour, int minute, bool use24Hour) =>
        Formatter.Format(
            new DateTime(2026, 11, 26, hour, minute, 0),
            new TimeFormatOptions(Natural: false, Use24Hour: use24Hour));

    [Theory]
    [InlineData(9, 0, "Son las nueve.")]
    [InlineData(9, 15, "Son las nueve y cuarto.")]
    [InlineData(9, 30, "Son las nueve y media.")]
    [InlineData(9, 45, "Son las diez menos cuarto.")]
    [InlineData(9, 5, "Son las nueve y cinco.")]
    [InlineData(9, 20, "Son las nueve y veinte.")]
    [InlineData(9, 40, "Son las diez menos veinte.")]
    [InlineData(9, 59, "Son las diez menos uno.")]
    [InlineData(1, 0, "Es la una.")]
    [InlineData(1, 15, "Es la una y cuarto.")]
    [InlineData(0, 0, "Es medianoche.")]
    [InlineData(12, 0, "Es mediodía.")]
    [InlineData(12, 30, "Son las doce y media.")]
    [InlineData(12, 45, "Es la una menos cuarto.")]
    [InlineData(0, 30, "Son las doce y media.")]
    [InlineData(23, 45, "Son las doce menos cuarto.")]
    [InlineData(19, 45, "Son las ocho menos cuarto.")]
    [InlineData(23, 59, "Son las doce menos uno.")]
    public void NaturalTwelveHour_UsesSpanishClockPhrasing(int hour, int minute, string expected) =>
        Assert.Equal(expected, Natural(hour, minute));

    [Theory]
    [InlineData(19, 45, "Son las diecinueve y cuarenta y cinco.")]
    [InlineData(21, 0, "Son las veintiuna.")]
    [InlineData(0, 0, "Es medianoche.")]
    [InlineData(0, 5, "Son las cero y cinco.")]
    [InlineData(1, 5, "Es la una y cinco.")]
    [InlineData(12, 0, "Son las doce.")]
    [InlineData(23, 59, "Son las veintitrés y cincuenta y nueve.")]
    public void TwentyFourHour_ReadsTheLiteralTwentyFourHourTime(int hour, int minute, string expected) =>
        Assert.Equal(expected, Exact(hour, minute, use24Hour: true));

    [Theory]
    [InlineData(19, 45, "Son las siete y cuarenta y cinco.")]
    [InlineData(12, 5, "Son las doce y cinco.")]
    [InlineData(0, 5, "Son las doce y cinco.")]
    [InlineData(9, 30, "Son las nueve y treinta.")]
    [InlineData(1, 0, "Es la una.")]
    public void ExactTwelveHour_SpellsEveryNumber(int hour, int minute, string expected) =>
        Assert.Equal(expected, Exact(hour, minute, use24Hour: false));

    [Fact]
    public void OneOClock_IsSingularAndHoursAreFeminine()
    {
        Assert.Equal("Es la una.", Natural(1, 0));
        Assert.Equal("Es la una y media.", Natural(13, 30));
        Assert.Equal("Son las veintiuna.", Exact(21, 0, use24Hour: true));
    }

    [Fact]
    public void EveryMinute_ProducesASpanishSentence()
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
                        phrase.StartsWith("Es ", StringComparison.Ordinal) ||
                        phrase.StartsWith("Son las ", StringComparison.Ordinal),
                        phrase);
                    Assert.EndsWith(".", phrase, StringComparison.Ordinal);
                    Assert.DoesNotContain("o'clock", phrase);
                    Assert.DoesNotContain("  ", phrase);
                }
            }
        }
    }
}
