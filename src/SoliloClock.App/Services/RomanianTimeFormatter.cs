using System;
using System.Globalization;

namespace SoliloClock.Services;

/// <summary>
/// Romanian clock phrasing: "Este ora nouă", "și un sfert", "și jumătate", "zece
/// fără un sfert". Minutes to the hour use "fără", and numbers of twenty or more
/// take the "de" link ("fără douăzeci de minute").
/// </summary>
public sealed class RomanianTimeFormatter : ITimeFormatter
{
    private static readonly string[] Hours12 =
    {
        "douăsprezece", "unu", "doi", "trei", "patru", "cinci", "șase", "șapte", "opt", "nouă", "zece", "unsprezece",
    };

    private static readonly string[] Ones =
    {
        "zero", "unu", "doi", "trei", "patru", "cinci", "șase", "șapte", "opt", "nouă",
        "zece", "unsprezece", "doisprezece", "treisprezece", "paisprezece", "cincisprezece", "șaisprezece", "șaptesprezece", "optsprezece", "nouăsprezece",
    };

    private static readonly string[] Tens = { "zero", "zece", "douăzeci", "treizeci", "patruzeci", "cincizeci" };

    public string Format(DateTime localTime, TimeFormatOptions options)
    {
        if (localTime.Hour == 0 && localTime.Minute == 0)
        {
            return "Este miezul nopții.";
        }

        if (options.Use24Hour)
        {
            var hour24 = Number(localTime.Hour);
            return localTime.Minute == 0
                ? $"Este ora {hour24}."
                : $"Este ora {hour24} și {Minutes(localTime.Minute)}.";
        }

        if (!options.Natural)
        {
            var exactHour = HourName(localTime.Hour);
            return localTime.Minute == 0
                ? $"Este ora {exactHour}."
                : $"Este ora {exactHour} și {Minutes(localTime.Minute)}.";
        }

        return Natural(localTime.Hour, localTime.Minute);
    }

    private static string Natural(int hour24, int minute)
    {
        var hour = HourName(hour24);
        var next = HourName((hour24 + 1) % 24);

        if (minute == 0)
        {
            return hour24 == 12 ? "Este amiază." : $"Este ora {hour}.";
        }

        return minute switch
        {
            15 => $"Este ora {hour} și un sfert.",
            30 => $"Este ora {hour} și jumătate.",
            45 => $"Este ora {next} fără un sfert.",
            < 30 => $"Este ora {hour} și {Minutes(minute)}.",
            _ => $"Este ora {next} fără {MinutesTo(60 - minute)}.",
        };
    }

    private static string HourName(int hour24) => Hours12[hour24 % 12];

    /// <summary>"un minut", "cinci minute", "douăzeci de minute".</summary>
    private static string Minutes(int value) => value == 1 ? "un minut" : Linked(value);

    private static string MinutesTo(int value) => value == 1 ? "un minut" : Linked(value);

    /// <summary>Romanian needs "de" before the noun from twenty up.</summary>
    private static string Linked(int value) =>
        value >= 20 ? $"{Number(value)} de minute" : $"{Number(value)} minute";

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
        return ones == 0 ? Tens[tens] : $"{Tens[tens]} și {Ones[ones]}";
    }
}
