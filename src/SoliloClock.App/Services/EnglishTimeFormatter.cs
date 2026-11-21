using System;

namespace SoliloClock.Services;

public sealed record TimeFormatOptions(bool Natural, bool Use24Hour);

public interface ITimeFormatter
{
    string Format(DateTime localTime, TimeFormatOptions options);
}

/// <summary>
/// English time phrasing. The M1 build ships English only; later locales get
/// their own formatters rather than sharing English grammar.
/// </summary>
public sealed class EnglishTimeFormatter : ITimeFormatter
{
    private static readonly string[] Ones =
    {
        "zero", "one", "two", "three", "four", "five", "six", "seven", "eight", "nine",
        "ten", "eleven", "twelve", "thirteen", "fourteen", "fifteen", "sixteen",
        "seventeen", "eighteen", "nineteen",
    };

    private static readonly string[] Tens =
    {
        "zero", "ten", "twenty", "thirty", "forty", "fifty",
    };

    public string Format(DateTime localTime, TimeFormatOptions options)
    {
        if (options.Use24Hour)
        {
            // A 24-hour preference is spoken as exact 24-hour time. SAPI reads
            // "19:45" naturally as "nineteen forty-five".
            return $"It's {localTime.Hour:00}:{localTime.Minute:00}.";
        }

        if (!options.Natural)
        {
            var exactHour = To12Hour(localTime.Hour);
            return $"It's {exactHour}:{localTime.Minute:00}.";
        }

        return FormatNatural12Hour(localTime);
    }

    private static string FormatNatural12Hour(DateTime time)
    {
        var hour = To12Hour(time.Hour);
        var nextHour = hour == 12 ? 1 : hour + 1;
        var minute = time.Minute;

        return minute switch
        {
            0 => $"It's {NumberToWords(hour)} o'clock.",
            15 => $"It's quarter past {NumberToWords(hour)}.",
            30 => $"It's half past {NumberToWords(hour)}.",
            45 => $"It's quarter to {NumberToWords(nextHour)}.",
            < 30 => $"It's {NumberToWords(minute)} past {NumberToWords(hour)}.",
            _ => $"It's {NumberToWords(60 - minute)} to {NumberToWords(nextHour)}.",
        };
    }

    private static int To12Hour(int hour24)
    {
        var hour = hour24 % 12;
        return hour == 0 ? 12 : hour;
    }

    /// <summary>Spells 0-59 with hyphens, e.g. "twenty-five".</summary>
    public static string NumberToWords(int value)
    {
        if (value is < 0 or > 59)
        {
            return value.ToString(System.Globalization.CultureInfo.InvariantCulture);
        }

        if (value < 20)
        {
            return Ones[value];
        }

        var tens = value / 10;
        var ones = value % 10;
        return ones == 0 ? Tens[tens] : $"{Tens[tens]}-{Ones[ones]}";
    }
}
