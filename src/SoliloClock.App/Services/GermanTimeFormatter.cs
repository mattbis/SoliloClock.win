using System;

namespace SoliloClock.Services;

/// <summary>
/// German clock phrasing: "Es ist neun Uhr", "Viertel nach neun", "halb zehn"
/// (halfway to ten, not half past nine), "Viertel vor zehn". Other minutes are
/// read as clock time ("neun Uhr zwanzig"), which is understood everywhere,
/// rather than a region-specific "zwanzig nach neun" / "zwanzig vor halb zehn".
/// A 24-hour preference is spoken literally: "neunzehn Uhr fünfundvierzig".
/// </summary>
public sealed class GermanTimeFormatter : ITimeFormatter
{
    private static readonly string[] Ones =
    {
        "null", "eins", "zwei", "drei", "vier", "fünf", "sechs", "sieben", "acht",
        "neun", "zehn", "elf", "zwölf", "dreizehn", "vierzehn", "fünfzehn",
        "sechzehn", "siebzehn", "achtzehn", "neunzehn",
    };

    private static readonly string[] Tens =
    {
        "null", "zehn", "zwanzig", "dreißig", "vierzig", "fünfzig",
    };

    /// <summary>"einundzwanzig", not "einsundzwanzig".</summary>
    private static readonly string[] LinkingOnes =
    {
        "null", "ein", "zwei", "drei", "vier", "fünf", "sechs", "sieben", "acht",
        "neun", "zehn", "elf", "zwölf", "dreizehn", "vierzehn", "fünfzehn",
        "sechzehn", "siebzehn", "achtzehn", "neunzehn",
    };

    public string Format(DateTime localTime, TimeFormatOptions options)
    {
        if (options.Use24Hour)
        {
            return $"Es ist {Clock(localTime.Hour, localTime.Minute)}.";
        }

        if (!options.Natural)
        {
            return $"Es ist {Clock(To12Hour(localTime.Hour, localTime.Minute), localTime.Minute)}.";
        }

        return $"Es ist {Natural(localTime.Hour, localTime.Minute)}.";
    }

    private static string Natural(int hour24, int minute)
    {
        if (minute == 0 && hour24 == 0)
        {
            return "Mitternacht";
        }

        if (minute == 0 && hour24 == 12)
        {
            return "Mittag";
        }

        var hour = To12Hour(hour24, minute);
        var next = To12Hour((hour24 + 1) % 24, 0);

        // "halb eins", not "halb ein": only "X Uhr" uses the "ein" form.
        return minute switch
        {
            15 => $"Viertel nach {NumberToWords(hour)}",
            30 => $"halb {NumberToWords(next)}",
            45 => $"Viertel vor {NumberToWords(next)}",
            _ => Clock(hour, minute),
        };
    }

    /// <summary>"neun Uhr", "neun Uhr fünf", "ein Uhr", "null Uhr".</summary>
    private static string Clock(int hour, int minute) => minute == 0
        ? $"{HourWord(hour)} Uhr"
        : $"{HourWord(hour)} Uhr {NumberToWords(minute)}";

    /// <summary>1–12 for the 12-hour clock.</summary>
    private static int To12Hour(int hour24, int minute)
    {
        var hour = hour24 % 12;
        return hour == 0 ? 12 : hour;
    }

    private static string HourWord(int hour) => hour == 1 ? "ein" : NumberToWords(hour);

    /// <summary>Spells 0–59, compounding the way German does: "fünfundvierzig".</summary>
    private static string NumberToWords(int value)
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
        return ones == 0 ? Tens[tens] : $"{LinkingOnes[ones]}und{Tens[tens]}";
    }
}
