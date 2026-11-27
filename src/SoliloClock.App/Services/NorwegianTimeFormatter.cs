using System;
using System.Globalization;

namespace SoliloClock.Services;

/// <summary>
/// Norwegian Bokmål clock phrasing: "Klokka er ni", "fem over ni", "kvart over
/// ni", "halv ti" and "kvart på ti" (quarter to ten). The half hour points at
/// the coming hour, as in Danish and Swedish.
/// </summary>
public sealed class NorwegianTimeFormatter : ITimeFormatter
{
    private static readonly string[] Hours12 =
    {
        "tolv", "ett", "to", "tre", "fire", "fem", "seks", "sju", "åtte", "ni", "ti", "elleve",
    };

    private static readonly string[] Ones =
    {
        "null", "en", "to", "tre", "fire", "fem", "seks", "sju", "åtte", "ni",
        "ti", "elleve", "tolv", "tretten", "fjorten", "femten", "seksten", "sytten", "atten", "nitten",
    };

    private static readonly string[] Tens = { "null", "ti", "tjue", "tretti", "førti", "femti" };

    public string Format(DateTime localTime, TimeFormatOptions options)
    {
        if (localTime.Hour == 0 && localTime.Minute == 0)
        {
            return "Klokka er midnatt.";
        }

        if (options.Use24Hour)
        {
            return $"Klokka er {Exact24(localTime.Hour, localTime.Minute)}.";
        }

        if (!options.Natural)
        {
            return $"Klokka er {Exact12(localTime.Hour, localTime.Minute)}.";
        }

        return $"Klokka er {Natural(localTime.Hour, localTime.Minute)}.";
    }

    private static string Natural(int hour24, int minute)
    {
        if (minute == 0)
        {
            return hour24 == 12 ? "midt på dagen" : HourName(hour24);
        }

        var hour = HourName(hour24);
        var next = HourName((hour24 + 1) % 24);

        return minute switch
        {
            15 => $"kvart over {hour}",
            30 => $"halv {next}",
            45 => $"kvart på {next}",
            < 30 => $"{MinutePhrase(minute)} over {hour}",
            _ => $"{MinutePhrase(60 - minute)} på {next}",
        };
    }

    private static string Exact12(int hour24, int minute) =>
        minute == 0 ? HourName(hour24) : $"{HourName(hour24)} {Padded(minute)}";

    private static string Exact24(int hour24, int minute) =>
        minute == 0 ? Number(hour24) : $"{Number(hour24)} {Padded(minute)}";

    private static string HourName(int hour24) => Hours12[hour24 % 12];

    /// <summary>Clock speech drops the word for minutes: "fem over ni".</summary>
    private static string MinutePhrase(int value) =>
        value == 1 ? "ett minutt" : Number(value);

    private static string Padded(int minute) => minute < 10 ? $"null {Number(minute)}" : Number(minute);

    public static string Number(int value)
    {
        if (value is < 0 or > 59)
        {
            return value.ToString(CultureInfo.InvariantCulture);
        }

        if (value < 20)
        {
            return Ones[value];
        }

        var tens = value / 10;
        var ones = value % 10;
        return ones == 0 ? Tens[tens] : Tens[tens] + Ones[ones];
    }
}
