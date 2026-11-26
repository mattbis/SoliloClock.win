using System;

namespace SoliloClock.Services;

/// <summary>
/// Italian clock phrasing: "Sono le nove", "e un quarto", "e mezza", "meno un
/// quarto", with "È l'una" for one o'clock and named "mezzogiorno"/"mezzanotte".
/// The feminine "mezza" agrees with "ora"; a 24-hour preference is read
/// literally: "Sono le diciannove e quarantacinque".
/// </summary>
public sealed class ItalianTimeFormatter : ITimeFormatter
{
    private static readonly string[] Ones =
    {
        "zero", "uno", "due", "tre", "quattro", "cinque", "sei", "sette", "otto",
        "nove", "dieci", "undici", "dodici", "tredici", "quattordici", "quindici",
        "sedici", "diciassette", "diciotto", "diciannove",
    };

    private static readonly string[] Tens = { "zero", "dieci", "venti", "trenta", "quaranta", "cinquanta" };

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
            return "È mezzanotte.";
        }

        if (minute == 0 && hour24 == 12)
        {
            return "È mezzogiorno.";
        }

        var hour = To12Hour(hour24);
        var next = To12Hour((hour24 + 1) % 24);

        return minute switch
        {
            0 => Sentence(hour, string.Empty),
            15 => Sentence(hour, " e un quarto"),
            30 => Sentence(hour, " e mezza"),
            45 => Sentence(next, " meno un quarto"),
            < 30 => Sentence(hour, $" e {NumberToWords(minute)}"),
            _ => Sentence(next, $" meno {NumberToWords(60 - minute)}"),
        };
    }

    private static string Format24(int hour24, int minute)
    {
        if (minute == 0 && hour24 == 0)
        {
            return "È mezzanotte.";
        }

        return Sentence(hour24, minute == 0 ? string.Empty : $" e {NumberToWords(minute)}");
    }

    private static string Format12Exact(int hour24, int minute) =>
        Sentence(To12Hour(hour24), minute == 0 ? string.Empty : $" e {NumberToWords(minute)}");

    /// <summary>"È l'una" is singular; every other hour takes "Sono le".</summary>
    private static string Sentence(int hour, string tail) => hour == 1
        ? $"È l'una{tail}."
        : $"Sono le {NumberToWords(hour)}{tail}.";

    private static int To12Hour(int hour24) => hour24 % 12 == 0 ? 12 : hour24 % 12;

    /// <summary>
    /// Spells 0–59. Italian drops the final vowel of the tens before one and
    /// eight ("ventuno", "ventotto") and accents three ("ventitré").
    /// </summary>
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

        if (ones == 0)
        {
            return Tens[tens];
        }

        if (ones is 1 or 8)
        {
            return Tens[tens][..^1] + Ones[ones];
        }

        return ones == 3 ? $"{Tens[tens]}tré" : Tens[tens] + Ones[ones];
    }
}
