using System;
using SoliloClock.Services;
using Xunit;

namespace SoliloClock.Tests;

public sealed class PortugueseTimeFormatterTests
{
    private static readonly PortugueseTimeFormatter Formatter = new();

    private static string Natural(int hour, int minute) =>
        Formatter.Format(
            new DateTime(2026, 11, 26, hour, minute, 0),
            new TimeFormatOptions(Natural: true, Use24Hour: false));

    private static string Exact(int hour, int minute, bool use24Hour) =>
        Formatter.Format(
            new DateTime(2026, 11, 26, hour, minute, 0),
            new TimeFormatOptions(Natural: false, Use24Hour: use24Hour));

    [Theory]
    [InlineData(9, 0, "São nove horas.")]
    [InlineData(9, 15, "São nove horas e um quarto.")]
    [InlineData(9, 30, "São nove horas e meia.")]
    [InlineData(9, 45, "São dez horas menos um quarto.")]
    [InlineData(9, 5, "São nove horas e cinco.")]
    [InlineData(9, 20, "São nove horas e vinte.")]
    [InlineData(9, 40, "São dez horas menos vinte.")]
    [InlineData(9, 59, "São dez horas menos um.")]
    [InlineData(1, 0, "É uma hora.")]
    [InlineData(1, 15, "É uma hora e um quarto.")]
    [InlineData(0, 0, "É meia-noite.")]
    [InlineData(12, 0, "É meio-dia.")]
    [InlineData(12, 30, "São doze horas e meia.")]
    [InlineData(12, 45, "É uma hora menos um quarto.")]
    [InlineData(0, 30, "São doze horas e meia.")]
    [InlineData(23, 45, "São doze horas menos um quarto.")]
    [InlineData(19, 45, "São oito horas menos um quarto.")]
    [InlineData(23, 59, "São doze horas menos um.")]
    public void NaturalTwelveHour_UsesPortugueseClockPhrasing(int hour, int minute, string expected) =>
        Assert.Equal(expected, Natural(hour, minute));

    [Theory]
    [InlineData(19, 45, "São dezanove horas e quarenta e cinco.")]
    [InlineData(21, 0, "São vinte e uma horas.")]
    [InlineData(0, 0, "É meia-noite.")]
    [InlineData(0, 5, "São zero horas e cinco.")]
    [InlineData(1, 5, "É uma hora e cinco.")]
    [InlineData(12, 0, "São doze horas.")]
    [InlineData(23, 59, "São vinte e três horas e cinquenta e nove.")]
    public void TwentyFourHour_ReadsTheLiteralTwentyFourHourTime(int hour, int minute, string expected) =>
        Assert.Equal(expected, Exact(hour, minute, use24Hour: true));

    [Theory]
    [InlineData(19, 45, "São sete horas e quarenta e cinco.")]
    [InlineData(12, 5, "São doze horas e cinco.")]
    [InlineData(0, 5, "São doze horas e cinco.")]
    [InlineData(9, 30, "São nove horas e trinta.")]
    [InlineData(1, 0, "É uma hora.")]
    public void ExactTwelveHour_SpellsEveryNumber(int hour, int minute, string expected) =>
        Assert.Equal(expected, Exact(hour, minute, use24Hour: false));

    [Fact]
    public void HoursAgreeInTheFeminine()
    {
        Assert.Equal("São vinte e uma horas.", Exact(21, 0, use24Hour: true));
        Assert.Equal("São nove horas e vinte e um.", Natural(9, 21));
        Assert.Equal("São dez horas menos vinte e um.", Natural(9, 39));
    }

    [Fact]
    public void EveryMinute_ProducesAPortugueseSentence()
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
                        phrase.StartsWith("É ", StringComparison.Ordinal) ||
                        phrase.StartsWith("São ", StringComparison.Ordinal),
                        phrase);
                    Assert.EndsWith(".", phrase, StringComparison.Ordinal);
                    Assert.DoesNotContain("o'clock", phrase);
                    Assert.DoesNotContain("  ", phrase);
                }
            }
        }
    }
}
