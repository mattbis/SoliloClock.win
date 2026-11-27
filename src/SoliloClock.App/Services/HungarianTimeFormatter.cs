using System;
using System.Globalization;

namespace SoliloClock.Services;

/// <summary>
/// Hungarian clock phrasing, which names the coming hour: "Negyed tíz" (quarter
/// of ten, i.e. 9:15), "Fél tíz" (9:30) and "Háromnegyed tíz" (9:45). Minutes
/// past use "perccel múlt", minutes to use "perc múlva ... óra".
/// </summary>
public sealed class HungarianTimeFormatter : ITimeFormatter
{
    private static readonly string[] Hours12 =
    {
        "tizenkettő", "egy", "két", "három", "négy", "öt", "hat", "hét", "nyolc", "kilenc", "tíz", "tizenegy",
    };

    private static readonly string[] Ones =
    {
        "nulla", "egy", "kettő", "három", "négy", "öt", "hat", "hét", "nyolc", "kilenc",
        "tíz", "tizenegy", "tizenkettő", "tizenhárom", "tizennégy", "tizenöt", "tizenhat", "tizenhét", "tizennyolc", "tizenkilenc",
    };

    private static readonly string[] Tens = { "nulla", "tíz", "húsz", "harminc", "negyven", "ötven" };

    public string Format(DateTime localTime, TimeFormatOptions options)
    {
        if (localTime.Hour == 0 && localTime.Minute == 0)
        {
            return "Éjfél van.";
        }

        if (options.Use24Hour)
        {
            return Sentence(localTime.Minute == 0
                ? $"{Number(localTime.Hour)} óra."
                : $"{Number(localTime.Hour)} óra {Number(localTime.Minute)} perc.");
        }

        if (!options.Natural)
        {
            return Sentence(localTime.Minute == 0
                ? $"{HourName(localTime.Hour)} óra."
                : $"{HourName(localTime.Hour)} óra {Number(localTime.Minute)} perc.");
        }

        return Sentence(Natural(localTime.Hour, localTime.Minute));
    }

    private static string Natural(int hour24, int minute)
    {
        var hour = HourName(hour24);
        var next = HourName((hour24 + 1) % 24);

        if (minute == 0)
        {
            return hour24 == 12 ? "Dél van." : $"{hour} óra van.";
        }

        return minute switch
        {
            15 => $"Negyed {next} van.",
            30 => $"Fél {next} van.",
            45 => $"Háromnegyed {next} van.",
            < 30 => $"{Number(minute)} perccel múlt {hour}.",
            _ => $"{Number(60 - minute)} perc múlva {next} óra.",
        };
    }

    private static string HourName(int hour24) => Hours12[hour24 % 12];

    /// <summary>Hungarian sentences start with the hour word, which is lowercase in the tables.</summary>
    private static string Sentence(string text) =>
        text.Length == 0 || char.IsUpper(text[0]) ? text : char.ToUpperInvariant(text[0]) + text[1..];

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
        return ones == 0 ? Tens[tens] : $"{Tens[tens]}{Ones[ones]}";
    }
}
