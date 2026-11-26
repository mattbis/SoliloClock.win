using System;

namespace SoliloClock.Services;

/// <summary>
/// French clock phrasing: "il est sept heures et quart", "huit heures moins le
/// quart", "midi et demi". Hours and minutes are feminine in French ("une
/// heure", "vingt et une"), and a day-part suffix is added only when the
/// 12-hour reading would otherwise be ambiguous, so 19:45 becomes "huit heures
/// moins le quart du soir" while 09:15 stays "neuf heures et quart".
/// </summary>
public sealed class FrenchTimeFormatter : ITimeFormatter
{
    private static readonly string[] Ones =
    {
        "zéro", "un", "deux", "trois", "quatre", "cinq", "six", "sept", "huit",
        "neuf", "dix", "onze", "douze", "treize", "quatorze", "quinze", "seize",
        "dix-sept", "dix-huit", "dix-neuf",
    };

    private static readonly string[] Tens =
    {
        "zéro", "dix", "vingt", "trente", "quarante", "cinquante",
    };

    public string Format(DateTime localTime, TimeFormatOptions options)
    {
        if (options.Use24Hour)
        {
            // 24-hour preference is read as exact 24-hour time: "dix-neuf heures
            // quarante-cinq", not a 12-hour phrase with a suffix.
            var hour = HourPhrase24(localTime.Hour);
            return $"Il est {WithMinute(hour, localTime.Hour, localTime.Minute)}.";
        }

        if (!options.Natural)
        {
            var hour = HourPhrase12(localTime.Hour);
            return $"Il est {WithMinute(hour, localTime.Hour, localTime.Minute)}.";
        }

        return $"Il est {Natural(localTime.Hour, localTime.Minute)}.";
    }

    private static string Natural(int hour24, int minute)
    {
        var hour = HourPhrase12(hour24);

        switch (minute)
        {
            case 0:
                return hour + DayPart(hour24);
            case 1:
                return WithMinute(hour, hour24, minute) + DayPart(hour24);
            case 15:
                return $"{hour} et quart{DayPart(hour24)}";
            case 30:
                return $"{hour} et {(IsNamedHour(hour24) ? "demi" : "demie")}{DayPart(hour24)}";
            case 45:
            {
                var next = (hour24 + 1) % 24;
                return $"{HourPhrase12(next)} moins le quart{DayPart(hour24, next)}";
            }
            case < 30:
                return $"{hour} {NumberToWords(minute)}{DayPart(hour24)}";
            default:
            {
                var next = (hour24 + 1) % 24;
                return $"{HourPhrase12(next)} moins {NumberToWords(60 - minute)}{DayPart(hour24, next)}";
            }
        }
    }

    private static string WithMinute(string hour, int hour24, int minute)
    {
        if (minute == 0)
        {
            return hour;
        }

        if (minute == 1)
        {
            return IsNamedHour(hour24) ? $"{hour} et une" : $"{hour} une";
        }

        return $"{hour} {NumberToWords(minute)}";
    }

    /// <summary>
    /// "midi"/"minuit" are named hours and never take "heures" or a day part.
    /// A minute-to phrase drops the day part too, because "midi moins le quart"
    /// and "une heure moins le quart" are already unambiguous.
    /// </summary>
    private static string DayPart(int hour24) => IsNamedHour(hour24)
        ? string.Empty
        : hour24 switch
        {
            >= 1 and <= 5 => " du matin",
            >= 6 and <= 11 => string.Empty,
            >= 13 and <= 17 => " de l’après-midi",
            _ => " du soir",
        };

    private static string DayPart(int hour24, int nextHour24) =>
        IsNamedHour(hour24) || IsNamedHour(nextHour24) ? string.Empty : DayPart(hour24);

    private static bool IsNamedHour(int hour24) => hour24 is 0 or 12;

    private static string HourPhrase24(int hour24) => hour24 switch
    {
        0 => "minuit",
        12 => "midi",
        1 => "une heure",
        _ => $"{NumberToWords(hour24)} heures",
    };

    private static string HourPhrase12(int hour24)
    {
        if (hour24 == 0)
        {
            return "minuit";
        }

        if (hour24 == 12)
        {
            return "midi";
        }

        var hour = hour24 % 12;
        return hour == 1 ? "une heure" : $"{NumberToWords(hour)} heures";
    }

    /// <summary>
    /// Spells 0-59. French hours and minutes are feminine, so 1, 21, 31, 41 and
    /// 51 use "une": "vingt et une heures", "moins une".
    /// </summary>
    private static string NumberToWords(int value)
    {
        if (value is < 0 or > 59)
        {
            return value.ToString(System.Globalization.CultureInfo.InvariantCulture);
        }

        if (value < 20)
        {
            return value == 1 ? "une" : Ones[value];
        }

        var tens = value / 10;
        var ones = value % 10;

        if (ones == 0)
        {
            return Tens[tens];
        }

        if (ones == 1)
        {
            return $"{Tens[tens]} et une";
        }

        return $"{Tens[tens]}-{Ones[ones]}";
    }
}
