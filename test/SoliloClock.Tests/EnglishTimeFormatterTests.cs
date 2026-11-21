using System;
using SoliloClock.Services;
using Xunit;

namespace SoliloClock.Tests;

public sealed class EnglishTimeFormatterTests
{
    private static readonly EnglishTimeFormatter Formatter = new();

    private static string Format(int hour, int minute, bool natural = true, bool use24Hour = false) =>
        Formatter.Format(
            new DateTime(2026, 11, 21, hour, minute, 0),
            new TimeFormatOptions(natural, use24Hour));

    [Theory]
    [InlineData(7, 0, "It's seven o'clock.")]
    [InlineData(7, 15, "It's quarter past seven.")]
    [InlineData(7, 30, "It's half past seven.")]
    [InlineData(7, 45, "It's quarter to eight.")]
    [InlineData(7, 5, "It's five past seven.")]
    [InlineData(7, 25, "It's twenty-five past seven.")]
    [InlineData(7, 40, "It's twenty to eight.")]
    [InlineData(0, 15, "It's quarter past twelve.")]
    [InlineData(12, 30, "It's half past twelve.")]
    [InlineData(23, 45, "It's quarter to twelve.")]
    public void NaturalTwelveHour_MatchesExpectedPhrasing(int hour, int minute, string expected)
    {
        Assert.Equal(expected, Format(hour, minute));
    }

    [Theory]
    [InlineData(7, 15, "It's 7:15.")]
    [InlineData(19, 45, "It's 7:45.")]
    public void ExactTwelveHour_UsesNumericTime(int hour, int minute, string expected)
    {
        Assert.Equal(expected, Format(hour, minute, natural: false));
    }

    [Theory]
    [InlineData(7, 15, "It's 07:15.")]
    [InlineData(19, 45, "It's 19:45.")]
    [InlineData(0, 0, "It's 00:00.")]
    public void TwentyFourHour_UsesExactTimeEvenWhenNaturalIsRequested(int hour, int minute, string expected)
    {
        Assert.Equal(expected, Format(hour, minute, use24Hour: true));
    }

    [Theory]
    [InlineData(0, "zero")]
    [InlineData(13, "thirteen")]
    [InlineData(20, "twenty")]
    [InlineData(21, "twenty-one")]
    [InlineData(59, "fifty-nine")]
    public void NumberToWords_SpellsMinutes(int value, string expected)
    {
        Assert.Equal(expected, EnglishTimeFormatter.NumberToWords(value));
    }
}
