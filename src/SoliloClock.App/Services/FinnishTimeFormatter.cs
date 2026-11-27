using System;
using System.Globalization;

namespace SoliloClock.Services;

/// <summary>
/// Finnish clock phrasing: "Kello on yhdeksän", "viisi yli yhdeksän", "varttia
/// yli yhdeksän", "puoli kymmenen" (half to ten) and "varttia vaille kymmenen".
/// The coming hour is in the genitive after "puoli" and "vaille".
/// </summary>
public sealed class FinnishTimeFormatter : ITimeFormatter
{
    private static readonly string[] HoursNom =
    {
        "yksi", "kaksi", "kolme", "neljä", "viisi", "kuusi", "seitsemän", "kahdeksan", "yhdeksän", "kymmenen", "yksitoista", "kaksitoista",
    };

    /// <summary>Genitive hours, used after "puoli" and "vaille".</summary>
    private static readonly string[] HoursGen =
    {
        "yhden", "kahden", "kolmen", "neljän", "viiden", "kuuden", "seitsemän", "kahdeksan", "yhdeksän", "kymmenen", "yhdentoista", "kahdentoista",
    };

    private static readonly string[] Ones =
    {
        "nolla", "yksi", "kaksi", "kolme", "neljä", "viisi", "kuusi", "seitsemän", "kahdeksan", "yhdeksän",
        "kymmenen", "yksitoista", "kaksitoista", "kolmetoista", "neljätoista", "viisitoista", "kuusitoista", "seitsemäntoista", "kahdeksantoista", "yhdeksäntoista",
    };

    private static readonly string[] Tens = { "nolla", "kymmenen", "kaksikymmentä", "kolmekymmentä", "neljäkymmentä", "viisikymmentä" };

    public string Format(DateTime localTime, TimeFormatOptions options)
    {
        if (localTime.Hour == 0 && localTime.Minute == 0)
        {
            return "Kello on keskiyö.";
        }

        if (options.Use24Hour)
        {
            return localTime.Minute == 0
                ? $"Kello on {Number(localTime.Hour)}."
                : $"Kello on {Number(localTime.Hour)} {Number(localTime.Minute)}.";
        }

        if (!options.Natural)
        {
            var hour = HourName(localTime.Hour);
            return localTime.Minute == 0
                ? $"Kello on {hour}."
                : $"Kello on {hour} {Number(localTime.Minute)} minuuttia.";
        }

        return Natural(localTime.Hour, localTime.Minute);
    }

    private static string Natural(int hour24, int minute)
    {
        var hour = HourName(hour24);
        var next = HoursGen[Slot(hour24 + 1)];

        if (minute == 0)
        {
            return hour24 == 12 ? "Kello on keskipäivä." : $"Kello on {hour}.";
        }

        if (minute == 15)
        {
            return $"Kello on varttia yli {hour}.";
        }

        if (minute == 30)
        {
            return $"Kello on puoli {next}.";
        }

        if (minute == 45)
        {
            return $"Kello on varttia vaille {next}.";
        }

        return minute < 30
            ? $"Kello on {Amount(minute)} yli {hour}."
            : $"Kello on {Amount(60 - minute)} vaille {next}.";
    }

    private static string HourName(int hour24) => HoursNom[Slot(hour24)];

    /// <summary>Zero-based slot in the 1-12 tables, safe for hour 0 and hour 24.</summary>
    private static int Slot(int hour) => ((hour - 1) % 12 + 12) % 12;

    /// <summary>"minuutti" for one minute, otherwise the bare number.</summary>
    private static string Amount(int value) => value == 1 ? "minuutti" : Number(value);

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
