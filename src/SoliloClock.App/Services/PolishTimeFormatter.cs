using System;
using System.Globalization;

namespace SoliloClock.Services;

/// <summary>
/// Polish clock phrasing: "Jest dziewiąta", "kwadrans po dziewiątej", "wpół do
/// dziesiątej", "za kwadrans dziesiąta". Hour names are feminine ordinals
/// because they agree with "godzina", minutes after the half hour point at the
/// next hour, and minute nouns take Polish plural agreement ("minuta", "dwie
/// minuty", "pięć minut", "dwadzieścia dwie minuty").
/// </summary>
public sealed class PolishTimeFormatter : ITimeFormatter
{
    /// <summary>1-12 as clock hour names, indexed by <c>hour % 12</c>.</summary>
    private static readonly string[] Hour12 =
    {
        "dwunasta", "pierwsza", "druga", "trzecia", "czwarta", "piąta",
        "szósta", "siódma", "ósma", "dziewiąta", "dziesiąta", "jedenasta",
    };

    /// <summary>Same hours in the locative, the case "po" governs ("po dziewiątej").</summary>
    private static readonly string[] Hour12Locative =
    {
        "dwunastej", "pierwszej", "drugiej", "trzeciej", "czwartej", "piątej",
        "szóstej", "siódmej", "ósmej", "dziewiątej", "dziesiątej", "jedenastej",
    };

    /// <summary>The 24-hour reading uses the feminine ordinal for every hour.</summary>
    private static readonly string[] Hour24 =
    {
        "dwunasta", "pierwsza", "druga", "trzecia", "czwarta", "piąta", "szósta",
        "siódma", "ósma", "dziewiąta", "dziesiąta", "jedenasta", "dwunasta",
        "trzynasta", "czternasta", "piętnasta", "szesnasta", "siedemnasta",
        "osiemnasta", "dziewiętnasta", "dwudziesta", "dwudziesta pierwsza",
        "dwudziesta druga", "dwudziesta trzecia",
    };

    private static readonly string[] Ones =
    {
        "zero", "jeden", "dwa", "trzy", "cztery", "pięć", "sześć", "siedem", "osiem",
        "dziewięć", "dziesięć", "jedenaście", "dwanaście", "trzynaście", "czternaście",
        "piętnaście", "szesnaście", "siedemnaście", "osiemnaście", "dziewiętnaście",
    };

    private static readonly string[] Tens =
    {
        "zero", "dziesięć", "dwadzieścia", "trzydzieści", "czterdzieści", "pięćdziesiąt",
    };

    public string Format(DateTime localTime, TimeFormatOptions options)
    {
        if (options.Use24Hour)
        {
            return $"Jest {Exact24(localTime.Hour, localTime.Minute)}.";
        }

        if (!options.Natural)
        {
            return $"Jest {Exact12(localTime.Hour, localTime.Minute)}.";
        }

        return $"Jest {Natural(localTime.Hour, localTime.Minute)}.";
    }

    private static string Natural(int hour24, int minute)
    {
        var hour = HourName(hour24);
        var past = HourNameLocative(hour24);

        switch (minute)
        {
            case 0:
                return hour;
            case 15:
                return $"kwadrans po {past}";
            case 30:
                return $"wpół do {NextHourGenitive(hour24)}";
            case 45:
                return $"za kwadrans {NextHour(hour24)}";
            case 1:
                return $"minuta po {past}";
            case < 30:
                return $"{MinutesAfter(minute)} po {past}";
            default:
                return $"za {MinutesTo(60 - minute)} {NextHour(hour24)}";
        }
    }

    private static string Exact12(int hour24, int minute)
    {
        var hour = hour24 == 0 && minute > 0 ? Hour12[0] : HourName(hour24);
        return minute == 0 ? hour : $"{hour} {TwoDigits(minute)}";
    }

    private static string Exact24(int hour24, int minute)
    {
        if (hour24 == 0 && minute == 0)
        {
            return "północ";
        }

        var hour = Hour24[hour24];
        return minute == 0 ? hour : $"{hour} {TwoDigits(minute)}";
    }

    private static string HourName(int hour24) => hour24 switch
    {
        0 => "północ",
        12 => "południe",
        _ => Hour12[hour24 % 12],
    };

    private static string HourNameLocative(int hour24) => Hour12Locative[hour24 % 12];

    private static string NextHour(int hour24) => ((hour24 + 1) % 24) switch
    {
        0 => "północ",
        12 => "dwunasta",
        var next => Hour12[next % 12],
    };

    /// <summary>"wpół do" governs the genitive: "wpół do dziesiątej".</summary>
    private static string NextHourGenitive(int hour24) => ((hour24 + 1) % 24) switch
    {
        0 => "północy",
        var next => Hour12Locative[next % 12],
    };

    /// <summary>Minute count after the hour: "dwie minuty", "pięć minut".</summary>
    private static string MinutesAfter(int value) => value is 2 or 3 or 4 or 22 or 23 or 24
        ? $"{Feminine(value)} minuty"
        : $"{Number(value)} minut";

    /// <summary>Minute count before the hour: "za minutę", "za dwie minuty".</summary>
    private static string MinutesTo(int value) => value == 1
        ? "minutę"
        : MinutesAfter(value);

    /// <summary>"dwie" rather than "dwa", because minutes are feminine.</summary>
    private static string Feminine(int value)
    {
        if (value == 2)
        {
            return "dwie";
        }

        if (value < 20)
        {
            return Ones[value];
        }

        var tens = value / 10;
        var ones = value % 10;
        return ones == 0
            ? Tens[tens]
            : $"{Tens[tens]} {(ones == 2 ? "dwie" : Ones[ones])}";
    }

    private static string TwoDigits(int value)
    {
        var tens = value / 10;
        var ones = value % 10;

        if (tens == 0)
        {
            return $"zero {Ones[ones]}";
        }

        return ones == 0
            ? Tens[tens]
            : $"{Tens[tens]} {Ones[ones]}";
    }

    /// <summary>Spells 0-59 in the masculine form used for plain counts.</summary>
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
