using System;

namespace SoliloClock.Services;

/// <summary>
/// Dutch clock phrasing: "Het is negen uur", "kwart over negen", "half tien"
/// (halfway to ten), "kwart voor tien". Minutes outside the quarters use the
/// unambiguous "over"/"voor" forms ("twintig over negen"), avoiding the regional
/// split between "tien voor half" and "tien over". A 24-hour preference is read
/// literally: "negentien uur vijfenveertig". One o'clock and one minute use the
/// numeral "één" so they are not confused with the article "een".
/// </summary>
public sealed class DutchTimeFormatter : ITimeFormatter
{
    private static readonly string[] Ones =
    {
        "nul", "een", "twee", "drie", "vier", "vijf", "zes", "zeven", "acht",
        "negen", "tien", "elf", "twaalf", "dertien", "veertien", "vijftien",
        "zestien", "zeventien", "achttien", "negentien",
    };

    private static readonly string[] Tens = { "nul", "tien", "twintig", "dertig", "veertig", "vijftig" };

    public string Format(DateTime localTime, TimeFormatOptions options)
    {
        if (options.Use24Hour)
        {
            return Format24(localTime.Hour, localTime.Minute);
        }

        if (!options.Natural)
        {
            return Format12Exact(localTime.Hour, localTime.Minute);
        }

        return FormatNatural(localTime.Hour, localTime.Minute);
    }

    private static string FormatNatural(int hour24, int minute)
    {
        if (minute == 0 && hour24 == 0)
        {
            return "Het is middernacht.";
        }

        var hour = To12Hour(hour24);
        var next = To12Hour((hour24 + 1) % 24);

        var phrase = minute switch
        {
            0 => $"{HourWord(hour)} uur",
            15 => $"kwart over {HourWord(hour)}",
            30 => $"half {HourWord(next)}",
            45 => $"kwart voor {HourWord(next)}",
            < 30 => $"{MinuteWord(minute)} over {HourWord(hour)}",
            _ => $"{MinuteWord(60 - minute)} voor {HourWord(next)}",
        };

        return $"Het is {phrase}.";
    }

    private static string Format24(int hour24, int minute)
    {
        if (minute == 0 && hour24 == 0)
        {
            return "Het is middernacht.";
        }

        var clock = $"{HourWord(hour24)} uur";
        return minute == 0
            ? $"Het is {clock}."
            : $"Het is {clock} {NumberToWords(minute)}.";
    }

    private static string Format12Exact(int hour24, int minute)
    {
        var clock = $"{HourWord(To12Hour(hour24))} uur";
        return minute == 0
            ? $"Het is {clock}."
            : $"Het is {clock} {NumberToWords(minute)}.";
    }

    private static int To12Hour(int hour24) => hour24 % 12 == 0 ? 12 : hour24 % 12;

    private static string HourWord(int hour) => hour == 1 ? "één" : NumberToWords(hour);

    private static string MinuteWord(int minute) => minute == 1 ? "één" : NumberToWords(minute);

    /// <summary>
    /// Spells 0–59 the Dutch way: units before tens ("vijfenveertig"), with the
    /// linking "-en-" and a diaeresis on "tweeën"/"drieën".
    /// </summary>
    private static string NumberToWords(int value)
    {
        if (value is < 0 or > 59)
        {
            return value.ToString(System.Globalization.CultureInfo.InvariantCulture);
        }

        if (value < 20)
        {
            return value == 1 ? "een" : Ones[value];
        }

        var tens = value / 10;
        var ones = value % 10;

        if (ones == 0)
        {
            return Tens[tens];
        }

        // Only twee/drie keep the diaeresis: tweeëntwintig, drieëntwintig,
        // but eenentwintig and vierentwintig.
        var linking = ones is 2 or 3 ? $"{Ones[ones]}ën" : $"{Ones[ones]}en";
        return linking + Tens[tens];
    }
}
