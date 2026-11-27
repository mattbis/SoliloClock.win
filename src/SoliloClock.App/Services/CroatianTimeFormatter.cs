using System;
using System.Globalization;

namespace SoliloClock.Services;

/// <summary>
/// Croatian clock phrasing: "Sada je devet sati", "devet sati i petnaest
/// minuta", "petnaest minuta do deset". The hour noun agrees with the count
/// (sat / sata / sati), and minutes two to four take the feminine "dvije".
/// </summary>
public sealed class CroatianTimeFormatter : ITimeFormatter
{
    private static readonly string[] Hours12 =
    {
        "dvanaest", "jedan", "dva", "tri", "četiri", "pet", "šest", "sedam", "osam", "devet", "deset", "jedanaest",
    };

    private static readonly string[] Ones =
    {
        "nula", "jedan", "dva", "tri", "četiri", "pet", "šest", "sedam", "osam", "devet",
        "deset", "jedanaest", "dvanaest", "trinaest", "četrnaest", "petnaest", "šesnaest", "sedamnaest", "osamnaest", "devetnaest",
    };

    private static readonly string[] Tens = { "nula", "deset", "dvadeset", "trideset", "četrdeset", "pedeset" };

    public string Format(DateTime localTime, TimeFormatOptions options)
    {
        if (localTime.Hour == 0 && localTime.Minute == 0)
        {
            return "Sada je ponoć.";
        }

        if (options.Use24Hour)
        {
            var hour24 = Number(localTime.Hour);
            return localTime.Minute == 0
                ? $"Sada je {hour24} sati."
                : $"Sada je {hour24} sati i {Minutes(localTime.Minute)}.";
        }

        if (!options.Natural)
        {
            var exactHour = HourName(localTime.Hour);
            return localTime.Minute == 0
                ? $"Sada je {exactHour} {HourNoun(localTime.Hour)}."
                : $"Sada je {exactHour} {HourNoun(localTime.Hour)} i {Minutes(localTime.Minute)}.";
        }

        return Natural(localTime.Hour, localTime.Minute);
    }

    private static string Natural(int hour24, int minute)
    {
        var hour = HourName(hour24);
        var noun = HourNoun(hour24);
        var next = HourName((hour24 + 1) % 24);

        if (minute == 0)
        {
            return hour24 == 12 ? "Sada je podne." : $"Sada je {hour} {noun}.";
        }

        if (minute == 30)
        {
            return $"Sada je {hour} {noun} i trideset minuta.";
        }

        if (minute == 45)
        {
            return $"Sada je petnaest minuta do {next}.";
        }

        if (minute < 30)
        {
            return $"Sada je {hour} {noun} i {Minutes(minute)}.";
        }

        return $"Sada je {MinutesTo(60 - minute)} do {next}.";
    }

    private static string HourName(int hour24) => Hours12[hour24 % 12];

    private static string HourNoun(int hour24)
    {
        var slot = hour24 % 12;
        return slot == 1 ? "sat" : slot is 2 or 3 or 4 ? "sata" : "sati";
    }

    private static string Minutes(int value) => value switch
    {
        1 => "jedna minuta",
        2 => "dvije minute",
        3 => "tri minute",
        4 => "četiri minute",
        _ => $"{Number(value)} minuta",
    };

    private static string MinutesTo(int value) => value switch
    {
        1 => "jedna minuta",
        2 => "dvije minute",
        3 => "tri minute",
        4 => "četiri minute",
        _ => $"{Number(value)} minuta",
    };

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
        return ones == 0 ? Tens[tens] : $"{Tens[tens]} {Ones[ones]}";
    }
}
