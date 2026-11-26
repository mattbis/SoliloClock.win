using System;
using System.Globalization;

namespace SoliloClock.Services;

/// <summary>
/// Russian clock phrasing: "Сейчас девять часов", "четверть десятого",
/// "половина десятого", "без четверти десять", "десять минут десятого". The
/// hour after the half uses the genitive ordinal ("десятого"), and hour and
/// minute nouns take Russian plural agreement ("час", "два часа", "пять
/// часов"; "минута", "две минуты", "пять минут").
/// </summary>
public sealed class RussianTimeFormatter : ITimeFormatter
{
    private const string Midnight = "полночь";
    private const string Noon = "полдень";

    /// <summary>1-12, indexed by <c>hour % 12</c>.</summary>
    private static readonly string[] Hours12 =
    {
        "двенадцать", "один", "два", "три", "четыре", "пять",
        "шесть", "семь", "восемь", "девять", "десять", "одиннадцать",
    };

    /// <summary>Genitive hours, the case "четверть"/"половина" govern.</summary>
    private static readonly string[] Hours12Genitive =
    {
        "двенадцатого", "первого", "второго", "третьего", "четвёртого", "пятого",
        "шестого", "седьмого", "восьмого", "девятого", "десятого", "одиннадцатого",
    };

    private static readonly string[] Ones =
    {
        "ноль", "один", "два", "три", "четыре", "пять", "шесть", "семь", "восемь",
        "девять", "десять", "одиннадцать", "двенадцать", "тринадцать", "четырнадцать",
        "пятнадцать", "шестнадцать", "семнадцать", "восемнадцать", "девятнадцать",
    };

    private static readonly string[] Tens =
    {
        "ноль", "десять", "двадцать", "тридцать", "сорок", "пятьдесят",
    };

    /// <summary>Genitive numerals 0-19, used by "без … минут".</summary>
    private static readonly string[] GenitiveOnes =
    {
        "ноля", "одной", "двух", "трёх", "четырёх", "пяти", "шести", "семи", "восьми",
        "девяти", "десяти", "одиннадцати", "двенадцати", "тринадцати", "четырнадцати",
        "пятнадцати", "шестнадцати", "семнадцати", "восемнадцати", "девятнадцати",
    };

    public string Format(DateTime localTime, TimeFormatOptions options)
    {
        if (options.Use24Hour)
        {
            return $"Сейчас {Exact24(localTime.Hour, localTime.Minute)}.";
        }

        if (!options.Natural)
        {
            return $"Сейчас {Exact12(localTime.Hour, localTime.Minute)}.";
        }

        return $"Сейчас {Natural(localTime.Hour, localTime.Minute)}.";
    }

    private static string Natural(int hour24, int minute)
    {
        var hour = HourWithNoun(hour24);
        var next = NextHourGenitive(hour24);

        switch (minute)
        {
            case 0:
                return hour;
            case 15:
                return $"четверть {next}";
            case 30:
                return $"половина {next}";
            case 45:
                return $"без четверти {NextHourNominative(hour24)}";
            case 1:
                return $"минута {next}";
            case < 30:
                return $"{MinutesPast(minute)} {next}";
            default:
                return $"без {MinutesTo(60 - minute)} {NextHourNominative(hour24)}";
        }
    }

    private static string Exact12(int hour24, int minute)
    {
        var hour = HourWithNoun(hour24);
        return minute == 0 ? hour : $"{hour} {TwoDigits(minute)}";
    }

    private static string Exact24(int hour24, int minute)
    {
        if (hour24 == 0 && minute == 0)
        {
            return Midnight;
        }

        var hour = $"{Number(hour24)} {HourNoun(hour24)}";
        return minute == 0 ? hour : $"{hour} {MinuteNoun(minute)}";
    }

    private static string HourWithNoun(int hour24) => hour24 switch
    {
        0 => Midnight,
        12 => Noon,
        // "час", never "один час".
        1 or 13 => "час",
        _ => $"{Hours12[hour24 % 12]} {HourNoun(hour24 % 12)}",
    };

    private static string HourNoun(int hour)
    {
        if (hour % 100 is >= 11 and <= 14)
        {
            return "часов";
        }

        return (hour % 10) switch
        {
            1 => "час",
            2 or 3 or 4 => "часа",
            _ => "часов",
        };
    }

    private static string NextHourNominative(int hour24)
    {
        var next = (hour24 + 1) % 24;
        return next == 1 ? "час" : Hours12[next % 12];
    }

    private static string NextHourGenitive(int hour24) => Hours12Genitive[(hour24 + 1) % 12];

    /// <summary>Minutes after the hour: "две минуты", "пять минут".</summary>
    private static string MinutesPast(int value)
    {
        if (value % 10 == 1 && value % 100 != 11)
        {
            return $"{Feminine(value)} минута";
        }

        return value is 2 or 3 or 4 or 22 or 23 or 24
            ? $"{Feminine(value)} минуты"
            : $"{Number(value)} минут";
    }

    /// <summary>Minutes before the hour, in the genitive: "без двух минут".</summary>
    private static string MinutesTo(int value) => value switch
    {
        1 => "одной минуты",
        < 20 => $"{GenitiveOnes[value]} минут",
        20 => "двадцати минут",
        21 => "двадцати одной минуты",
        _ => $"двадцати {GenitiveOnes[value - 20]} минут",
    };

    private static string MinuteNoun(int value) => value == 1
        ? "одна минута"
        : MinutesPast(value);

    /// <summary>"одна"/"две" rather than "один"/"два", because minutes are feminine.</summary>
    private static string Feminine(int value)
    {
        if (value == 1)
        {
            return "одна";
        }

        if (value == 2)
        {
            return "две";
        }

        if (value < 20)
        {
            return Ones[value];
        }

        var tens = value / 10;
        var ones = value % 10;
        return ones == 0
            ? Tens[tens]
            : $"{Tens[tens]} {(ones == 1 ? "одна" : ones == 2 ? "две" : Ones[ones])}";
    }

    private static string TwoDigits(int value)
    {
        var tens = value / 10;
        var ones = value % 10;

        if (tens == 0)
        {
            return $"ноль {Ones[ones]}";
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
