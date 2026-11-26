using System;
using SoliloClock.Services;
using Xunit;

namespace SoliloClock.Tests;

public sealed class WelshTimeFormatterTests
{
    private static readonly WelshTimeFormatter Formatter = new();

    private static string Natural(int hour, int minute) =>
        Formatter.Format(
            new DateTime(2026, 11, 26, hour, minute, 0),
            new TimeFormatOptions(Natural: true, Use24Hour: false));

    private static string Exact(int hour, int minute, bool use24Hour) =>
        Formatter.Format(
            new DateTime(2026, 11, 26, hour, minute, 0),
            new TimeFormatOptions(Natural: false, Use24Hour: use24Hour));

    [Theory]
    [InlineData(9, 0, "Mae'n naw o'r gloch.")]
    [InlineData(3, 0, "Mae'n dri o'r gloch.")]
    [InlineData(2, 0, "Mae'n ddau o'r gloch.")]
    [InlineData(1, 0, "Mae'n un o'r gloch.")]
    [InlineData(10, 0, "Mae'n ddeg o'r gloch.")]
    [InlineData(9, 15, "Mae'n chwarter wedi naw.")]
    [InlineData(9, 30, "Mae'n hanner awr wedi naw.")]
    [InlineData(9, 45, "Mae'n chwarter i ddeg.")]
    [InlineData(9, 5, "Mae'n bum munud wedi naw.")]
    [InlineData(9, 10, "Mae'n ddeng munud wedi naw.")]
    [InlineData(9, 20, "Mae'n ugain munud wedi naw.")]
    [InlineData(9, 40, "Mae'n ugain munud i ddeg.")]
    [InlineData(9, 50, "Mae'n ddeng munud i ddeg.")]
    [InlineData(9, 55, "Mae'n bum munud i ddeg.")]
    [InlineData(12, 0, "Mae'n hanner dydd.")]
    [InlineData(0, 0, "Mae'n hanner nos.")]
    [InlineData(12, 15, "Mae'n chwarter wedi deuddeg.")]
    [InlineData(0, 30, "Mae'n hanner awr wedi deuddeg.")]
    [InlineData(23, 45, "Mae'n chwarter i hanner nos.")]
    [InlineData(11, 45, "Mae'n chwarter i hanner dydd.")]
    public void NaturalTwelveHour_UsesWelshClockPhrasing(int hour, int minute, string expected) =>
        Assert.Equal(expected, Natural(hour, minute));

    [Theory]
    [InlineData(19, 45, "Mae'n bedwar ar bymtheg o'r gloch a phedwar deg pump munud.")]
    [InlineData(21, 0, "Mae'n un ar hugain o'r gloch.")]
    [InlineData(1, 5, "Mae'n un o'r gloch a phum munud.")]
    [InlineData(0, 0, "Mae'n hanner nos.")]
    [InlineData(0, 10, "Mae'n deng munud wedi hanner nos.")]
    [InlineData(12, 0, "Mae'n ddeuddeg o'r gloch.")]
    public void TwentyFourHour_ReadsExactTime(int hour, int minute, string expected) =>
        Assert.Equal(expected, Exact(hour, minute, use24Hour: true));

    [Theory]
    [InlineData(19, 45, "Mae'n saith o'r gloch pedwar deg pump munud.")]
    [InlineData(15, 0, "Mae'n dri o'r gloch.")]
    [InlineData(0, 0, "Mae'n ddeuddeg o'r gloch.")]
    public void ExactTwelveHour_SpellsEveryNumber(int hour, int minute, string expected) =>
        Assert.Equal(expected, Exact(hour, minute, use24Hour: false));

    [Fact]
    public void WelshPhrasing_DoesNotFallBackToEnglishWords()
    {
        foreach (var minute in new[] { 0, 15, 30, 45, 20, 40 })
        {
            var phrase = Natural(7, minute);

            Assert.StartsWith("Mae'n ", phrase);
            Assert.DoesNotContain("o'clock", phrase);
            Assert.DoesNotContain("past", phrase);
            Assert.DoesNotContain("quarter to", phrase);
        }
    }

    [Fact]
    public void EveryMinute_ProducesAWelshSentence()
    {
        for (var hour = 0; hour < 24; hour++)
        {
            for (var minute = 0; minute < 60; minute++)
            {
                var phrase = Natural(hour, minute);

                Assert.StartsWith("Mae'n ", phrase);
                Assert.EndsWith(".", phrase);
                Assert.DoesNotContain("  ", phrase);
            }
        }
    }
}
