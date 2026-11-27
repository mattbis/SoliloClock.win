using System;
using System.Globalization;

namespace SoliloClock.Services;

/// <summary>
/// Slovenian clock phrasing: "Zdaj je devet", "devet in petnajst", "devet in
/// pol" and "petnajst do deset". Numbers above twenty are ones-first
/// ("enaindvajset"), and minutes one to four agree with the feminine noun.
/// </summary>
public sealed class SlovenianTimeFormatter : ITimeFormatter
{
    private static readonly string[] Hours12 =
    {
        "dvanajst", "ena", "dva", "tri", "štiri", "pet", "šest", "sedem", "osem", "devet", "deset", "enajst",
    };

    private static readonly string[] Ones =
    {
        "nič", "ena", "dva", "tri", "štiri", "pet", "šest", "sedem", "osem", "devet",
        "deset", "enajst", "dvanajst", "trinajst", "štirinajst", "petnajst", "šestnajst", "sedemnajst", "osemnajst", "devetnajst",
    };

    private static readonly string[] Tens = { "nič", "deset", "dvajset", "trideset", "štirideset", "petdeset" };

    public string Format(DateTime localTime, TimeFormatOptions options)
    {
        if (localTime.Hour == 0 && localTime.Minute == 0)
        {
            return "Zdaj je polnoč.";
        }

        if (options.Use24Hour)
        {
            return localTime.Minute == 0
                ? $"Zdaj je {Number(localTime.Hour)}."
                : $"Zdaj je {Number(localTime.Hour)} {Number(localTime.Minute)}.";
        }

        if (!options.Natural)
        {
            return localTime.Minute == 0
                ? $"Zdaj je {HourName(localTime.Hour)}."
                : $"Zdaj je {HourName(localTime.Hour)} in {Minutes(localTime.Minute)}.";
        }

        var hour = HourName(localTime.Hour);
        var next = HourName((localTime.Hour + 1) % 24);

        if (localTime.Minute == 0)
        {
            return localTime.Hour == 12 ? "Zdaj je poldne." : $"Zdaj je {hour}.";
        }

        return localTime.Minute switch
        {
            15 => $"Zdaj je {hour} in petnajst.",
            30 => $"Zdaj je {hour} in pol.",
            45 => $"Zdaj je petnajst do {next}.",
            < 30 => $"Zdaj je {hour} in {Minutes(localTime.Minute)}.",
            _ => $"Zdaj je {Minutes(60 - localTime.Minute)} do {next}.",
        };
    }

    private static string HourName(int hour24) => Hours12[hour24 % 12];

    private static string Minutes(int value) => value switch
    {
        1 => "ena minuta",
        2 => "dve minuti",
        3 => "tri minute",
        4 => "štiri minute",
        _ => $"{Number(value)} minut",
    };

    /// <summary>Spells 0-59; Slovenian says the ones before the tens.</summary>
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
        return ones == 0 ? Tens[tens] : $"{Ones[ones]}in{Tens[tens]}";
    }
}
