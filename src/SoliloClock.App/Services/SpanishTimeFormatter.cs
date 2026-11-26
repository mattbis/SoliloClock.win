using System;

namespace SoliloClock.Services;

/// <summary>
/// Spanish clock phrasing: "Son las nueve", "y cuarto", "y media", "menos cuarto",
/// with the singular "Es la una" for one o'clock and named "mediodía"/"medianoche".
/// A 24-hour preference is read literally: "Son las diecinueve y cuarenta y cinco".
/// </summary>
public sealed class SpanishTimeFormatter : ITimeFormatter
{
    private static readonly string[] Ones =
    {
        "cero", "uno", "dos", "tres", "cuatro", "cinco", "seis", "siete", "ocho",
        "nueve", "diez", "once", "doce", "trece", "catorce", "quince", "dieciséis",
        "diecisiete", "dieciocho", "diecinueve",
    };

    private static readonly string[] Twenties =
    {
        "veinte", "veintiuno", "veintidós", "veintitrés", "veinticuatro",
        "veinticinco", "veintiséis", "veintisiete", "veintiocho", "veintinueve",
    };

    private static readonly string[] Tens = { "cero", "diez", "veinte", "treinta", "cuarenta", "cincuenta" };

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
            return "Es medianoche.";
        }

        if (minute == 0 && hour24 == 12)
        {
            return "Es mediodía.";
        }

        var hour = To12Hour(hour24);
        var next = To12Hour((hour24 + 1) % 24);

        return minute switch
        {
            0 => Sentence(hour, string.Empty),
            15 => Sentence(hour, " y cuarto"),
            30 => Sentence(hour, " y media"),
            45 => Sentence(next, " menos cuarto"),
            < 30 => Sentence(hour, $" y {NumberToWords(minute)}"),
            _ => Sentence(next, $" menos {NumberToWords(60 - minute)}"),
        };
    }

    private static string Format24(int hour24, int minute)
    {
        if (minute == 0 && hour24 == 0)
        {
            return "Es medianoche.";
        }

        return Sentence(hour24, minute == 0 ? string.Empty : $" y {NumberToWords(minute)}");
    }

    private static string Format12Exact(int hour24, int minute) =>
        Sentence(To12Hour(hour24), minute == 0 ? string.Empty : $" y {NumberToWords(minute)}");

    /// <summary>"Es la una", otherwise "Son las …": hours are feminine in Spanish.</summary>
    private static string Sentence(int hour, string tail) => hour == 1
        ? $"Es la una{tail}."
        : $"Son las {HourWords(hour)}{tail}.";

    /// <summary>Hours are feminine, so 21 is "veintiuna", not "veintiuno".</summary>
    private static string HourWords(int hour) => hour == 21 ? "veintiuna" : NumberToWords(hour);

    private static int To12Hour(int hour24) => hour24 % 12 == 0 ? 12 : hour24 % 12;

    /// <summary>Spells 0–59, with the fused twenties ("veintidós") and "treinta y uno".</summary>
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

        if (value < 30)
        {
            return Twenties[value - 20];
        }

        var tens = value / 10;
        var ones = value % 10;
        return ones == 0 ? Tens[tens] : $"{Tens[tens]} y {Ones[ones]}";
    }
}
