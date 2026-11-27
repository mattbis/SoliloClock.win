using System;
using System.Globalization;

namespace SoliloClock.Services;

/// <summary>
/// Catalan clock phrasing: "Són les nou", "les nou i quart", "les nou i mitja",
/// "les deu menys quart". One o'clock is singular ("És la una"), and midnight
/// and noon are named.
/// </summary>
public sealed class CatalanTimeFormatter : ITimeFormatter
{
    private static readonly string[] Hours12 =
    {
        "dotze", "una", "dues", "tres", "quatre", "cinc", "sis", "set", "vuit", "nou", "deu", "onze",
    };

    private static readonly string[] Ones =
    {
        "zero", "un", "dos", "tres", "quatre", "cinc", "sis", "set", "vuit", "nou",
        "deu", "onze", "dotze", "tretze", "catorze", "quinze", "setze", "disset", "divuit", "dinou",
    };

    private static readonly string[] Tens = { "zero", "deu", "vint", "trenta", "quaranta", "cinquanta" };

    public string Format(DateTime localTime, TimeFormatOptions options)
    {
        if (localTime.Hour == 0 && localTime.Minute == 0)
        {
            return "És mitjanit.";
        }

        if (options.Use24Hour)
        {
            return localTime.Minute == 0
                ? DaySentence(localTime.Hour)
                : DaySentence(localTime.Hour, $" i {Number(localTime.Minute)}");
        }

        if (!options.Natural)
        {
            return localTime.Minute == 0
                ? HourSentence(localTime.Hour, string.Empty)
                : HourSentence(localTime.Hour, $" i {Number(localTime.Minute)}");
        }

        if (localTime.Minute == 0)
        {
            return localTime.Hour == 12 ? "És migdia." : HourSentence(localTime.Hour, string.Empty);
        }

        var next = (localTime.Hour + 1) % 24;

        return localTime.Minute switch
        {
            15 => HourSentence(localTime.Hour, " i quart"),
            30 => HourSentence(localTime.Hour, " i mitja"),
            45 => HourSentence(next, " menys quart"),
            < 30 => HourSentence(localTime.Hour, $" i {Minutes(localTime.Minute)}"),
            _ => HourSentence(next, $" menys {Minutes(60 - localTime.Minute)}"),
        };
    }

    /// <summary>One o'clock takes the singular ("És la una").</summary>
    private static string HourSentence(int hour24, string rest)
    {
        var slot = hour24 % 12;
        var name = Hours12[slot];
        return slot == 1 ? $"És la {name}{rest}." : $"Són les {name}{rest}.";
    }

    /// <summary>24-hour phrasing keeps the digits' own name, only 01:00 is singular.</summary>
    private static string DaySentence(int hour24, string rest = "") =>
        hour24 == 1
            ? $"És la una{rest}."
            : $"Són les {Number(hour24)}{rest}.";

    private static string Minutes(int value) => value == 1 ? "un minut" : Number(value);

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
        if (ones == 0)
        {
            return Tens[tens];
        }

        // Only the twenties join with "-i-" (vint-i-un); higher tens hyphenate directly.
        return tens == 2 ? $"{Tens[tens]}-i-{Ones[ones]}" : $"{Tens[tens]}-{Ones[ones]}";
    }
}
