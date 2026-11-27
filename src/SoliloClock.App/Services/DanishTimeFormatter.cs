using System;
using System.Globalization;

namespace SoliloClock.Services;

/// <summary>
/// Danish clock phrasing: "Klokken er ni", "fem minutter over ni", "kvart over
/// ni", "halv ti" (half to ten, which is how Danish says half past nine) and
/// "kvart i ti". Numbers above twenty are said ones-first ("enogtyve").
/// </summary>
public sealed class DanishTimeFormatter : ITimeFormatter
{
    private static readonly string[] Hours12 =
    {
        "tolv", "et", "to", "tre", "fire", "fem", "seks", "syv", "otte", "ni", "ti", "elleve",
    };

    private static readonly string[] Ones =
    {
        "nul", "et", "to", "tre", "fire", "fem", "seks", "syv", "otte", "ni",
        "ti", "elleve", "tolv", "tretten", "fjorten", "femten", "seksten", "sytten", "atten", "nitten",
    };

    /// <summary>Danish tens, indexed by <c>value / 10</c>; 20 is "tyve", 50 "halvtreds".</summary>
    private static readonly string[] Tens = { "nul", "ti", "tyve", "tredive", "fyrre", "halvtreds" };

    public string Format(DateTime localTime, TimeFormatOptions options)
    {
        if (localTime.Hour == 0 && localTime.Minute == 0)
        {
            return "Klokken er midnat.";
        }

        if (options.Use24Hour)
        {
            return $"Klokken er {Exact24(localTime.Hour, localTime.Minute)}.";
        }

        if (!options.Natural)
        {
            return $"Klokken er {Exact12(localTime.Hour, localTime.Minute)}.";
        }

        return $"Klokken er {Natural(localTime.Hour, localTime.Minute)}.";
    }

    private static string Natural(int hour24, int minute)
    {
        if (minute == 0)
        {
            return hour24 == 12 ? "middag" : HourName(hour24);
        }

        var hour = HourName(hour24);
        var next = HourName((hour24 + 1) % 24);

        return minute switch
        {
            15 => $"kvart over {hour}",
            30 => $"halv {next}",
            45 => $"kvart i {next}",
            < 30 => $"{MinutePhrase(minute)} over {hour}",
            _ => $"{MinutePhrase(60 - minute)} i {next}",
        };
    }

    private static string Exact12(int hour24, int minute) =>
        minute == 0 ? HourName(hour24) : $"{HourName(hour24)} {Padded(minute)}";

    private static string Exact24(int hour24, int minute) =>
        minute == 0 ? Number(hour24) : $"{Number(hour24)} {Padded(minute)}";

    private static string HourName(int hour24) => Hours12[hour24 % 12];

    /// <summary>"et minut" for one, otherwise the plural ("fem minutter").</summary>
    private static string MinutePhrase(int value) =>
        value == 1 ? "et minut" : $"{Number(value)} minutter";

    /// <summary>Minutes below ten are read with a leading "nul" so 9:05 is not "ni fem".</summary>
    private static string Padded(int minute) => minute < 10 ? $"nul {Number(minute)}" : Number(minute);

    /// <summary>Spells 0-59; Danish says the ones before the tens.</summary>
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
        return ones == 0 ? Tens[tens] : $"{Ones[ones]}og{Tens[tens]}";
    }
}
