using System;

namespace SoliloClock.Services;

/// <summary>
/// European Portuguese clock phrasing: "São nove horas", "e um quarto", "e meia",
/// "menos um quarto", with "É uma hora" for one o'clock and named
/// "meio-dia"/"meia-noite". Hours are feminine, so 21 is "vinte e uma horas".
/// A 24-hour preference is read literally: "São dezanove horas e quarenta e cinco".
/// </summary>
public sealed class PortugueseTimeFormatter : ITimeFormatter
{
    private static readonly string[] Ones =
    {
        "zero", "um", "dois", "três", "quatro", "cinco", "seis", "sete", "oito",
        "nove", "dez", "onze", "doze", "treze", "catorze", "quinze", "dezasseis",
        "dezassete", "dezoito", "dezanove",
    };

    private static readonly string[] Tens = { "zero", "dez", "vinte", "trinta", "quarenta", "cinquenta" };

    public string Format(DateTime localTime, TimeFormatOptions options)
    {
        if (options.Use24Hour)
        {
            return Format24(localTime.Hour, localTime.Minute);
        }

        if (!options.Natural)
        {
            return Format12Exact(localTime.Hour, localTime.Minute);
        }

        return FormatNatural(localTime.Hour, localTime.Minute);
    }

    private static string FormatNatural(int hour24, int minute)
    {
        if (minute == 0 && hour24 == 0)
        {
            return "É meia-noite.";
        }

        if (minute == 0 && hour24 == 12)
        {
            return "É meio-dia.";
        }

        var hour = To12Hour(hour24);
        var next = To12Hour((hour24 + 1) % 24);

        return minute switch
        {
            0 => Sentence(hour, string.Empty),
            15 => Sentence(hour, " e um quarto"),
            30 => Sentence(hour, " e meia"),
            45 => Sentence(next, " menos um quarto"),
            < 30 => Sentence(hour, $" e {NumberToWords(minute)}"),
            _ => Sentence(next, $" menos {NumberToWords(60 - minute)}"),
        };
    }

    private static string Format24(int hour24, int minute)
    {
        if (minute == 0 && hour24 == 0)
        {
            return "É meia-noite.";
        }

        return Sentence(hour24, minute == 0 ? string.Empty : $" e {NumberToWords(minute)}");
    }

    private static string Format12Exact(int hour24, int minute) =>
        Sentence(To12Hour(hour24), minute == 0 ? string.Empty : $" e {NumberToWords(minute)}");

    /// <summary>"É uma hora" is singular; every other hour takes "São … horas".</summary>
    private static string Sentence(int hour, string tail) => hour == 1
        ? $"É uma hora{tail}."
        : $"São {HourWords(hour)} horas{tail}.";

    private static int To12Hour(int hour24) => hour24 % 12 == 0 ? 12 : hour24 % 12;

    /// <summary>Hours are feminine: "vinte e uma horas".</summary>
    private static string HourWords(int hour) => hour == 21 ? "vinte e uma" : NumberToWords(hour);

    /// <summary>Spells 0–59 with "e" between tens and units: "quarenta e cinco".</summary>
    private static string NumberToWords(int value)
    {
        if (value is < 0 or > 59)
        {
            return value.ToString(System.Globalization.CultureInfo.InvariantCulture);
        }

        if (value < 20)
        {
            return Ones[value];
        }

        var tens = value / 10;
        var ones = value % 10;
        return ones == 0 ? Tens[tens] : $"{Tens[tens]} e {Ones[ones]}";
    }
}
