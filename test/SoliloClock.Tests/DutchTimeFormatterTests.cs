using System;
using SoliloClock.Services;
using Xunit;

namespace SoliloClock.Tests;

public sealed class DutchTimeFormatterTests
{
    private static readonly DutchTimeFormatter Formatter = new();

    private static string Natural(int hour, int minute) =>
        Formatter.Format(
            new DateTime(2026, 11, 26, hour, minute, 0),
            new TimeFormatOptions(Natural: true, Use24Hour: false));

    private static string Exact(int hour, int minute, bool use24Hour) =>
        Formatter.Format(
            new DateTime(2026, 11, 26, hour, minute, 0),
            new TimeFormatOptions(Natural: false, Use24Hour: use24Hour));

    [Theory]
    [InlineData(9, 0, "Het is negen uur.")]
    [InlineData(9, 15, "Het is kwart over negen.")]
    [InlineData(9, 30, "Het is half tien.")]
    [InlineData(9, 45, "Het is kwart voor tien.")]
    [InlineData(9, 5, "Het is vijf over negen.")]
    [InlineData(9, 1, "Het is één over negen.")]
    [InlineData(9, 20, "Het is twintig over negen.")]
    [InlineData(9, 40, "Het is twintig voor tien.")]
    [InlineData(9, 59, "Het is één voor tien.")]
    [InlineData(1, 0, "Het is één uur.")]
    [InlineData(0, 0, "Het is middernacht.")]
    [InlineData(12, 0, "Het is twaalf uur.")]
    [InlineData(12, 30, "Het is half één.")]
    [InlineData(12, 45, "Het is kwart voor één.")]
    [InlineData(0, 30, "Het is half één.")]
    [InlineData(23, 45, "Het is kwart voor twaalf.")]
    [InlineData(19, 45, "Het is kwart voor acht.")]
    [InlineData(9, 28, "Het is achtentwintig over negen.")]
    public void NaturalTwelveHour_UsesDutchClockPhrasing(int hour, int minute, string expected) =>
        Assert.Equal(expected, Natural(hour, minute));

    [Theory]
    [InlineData(19, 45, "Het is negentien uur vijfenveertig.")]
    [InlineData(21, 0, "Het is eenentwintig uur.")]
    [InlineData(0, 0, "Het is middernacht.")]
    [InlineData(0, 5, "Het is nul uur vijf.")]
    [InlineData(1, 5, "Het is één uur vijf.")]
    [InlineData(12, 0, "Het is twaalf uur.")]
    [InlineData(23, 59, "Het is drieëntwintig uur negenenvijftig.")]
    public void TwentyFourHour_ReadsTheLiteralTwentyFourHourTime(int hour, int minute, string expected) =>
        Assert.Equal(expected, Exact(hour, minute, use24Hour: true));

    [Theory]
    [InlineData(19, 45, "Het is zeven uur vijfenveertig.")]
    [InlineData(12, 5, "Het is twaalf uur vijf.")]
    [InlineData(0, 5, "Het is twaalf uur vijf.")]
    [InlineData(9, 30, "Het is negen uur dertig.")]
    [InlineData(1, 0, "Het is één uur.")]
    public void ExactTwelveHour_SpellsEveryNumber(int hour, int minute, string expected) =>
        Assert.Equal(expected, Exact(hour, minute, use24Hour: false));

    [Fact]
    public void CompoundsUseTheDutchLinkingEnAndDiaeresis()
    {
        // eenentwintig (no diaeresis), tweeëntwintig, drieëntwintig, eenendertig.
        Assert.Equal("Het is eenentwintig uur.", Exact(21, 0, use24Hour: true));
        Assert.Equal("Het is tweeëntwintig uur.", Exact(22, 0, use24Hour: true));
        Assert.Equal("Het is drieëntwintig uur.", Exact(23, 0, use24Hour: true));
        Assert.Equal("Het is één uur vijfenvijftig.", Exact(1, 55, use24Hour: true));
        Assert.Equal("Het is achtentwintig over negen.", Natural(9, 28));
    }

    [Fact]
    public void EveryMinute_ProducesADutchSentence()
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

                    Assert.StartsWith("Het is ", phrase, StringComparison.Ordinal);
                    Assert.EndsWith(".", phrase, StringComparison.Ordinal);
                    Assert.DoesNotContain("o'clock", phrase);
                    Assert.DoesNotContain("  ", phrase);
                }
            }
        }
    }
}
