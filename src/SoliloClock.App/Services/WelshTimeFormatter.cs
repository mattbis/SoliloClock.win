using System;
using System.Globalization;

namespace SoliloClock.Services;

/// <summary>
/// Welsh clock phrasing: "Mae'n dri o'r gloch", "chwarter wedi naw", "hanner awr
/// wedi naw", "chwarter i ddeg". The hour is softly mutated after "yn" and after
/// "i" ("dri", "ddeg") but not after "wedi". Minutes count forward with "wedi"
/// up to the half hour and back from the next hour with "i", and the traditional
/// words "chwarter" and "hanner awr" stand in for 15 and 30.
/// </summary>
/// <remarks>
/// Draft wording: not yet reviewed by a native speaker. The cy-GB locale stays
/// flagged provisional until it is, so the app never claims a finished language.
/// </remarks>
public sealed class WelshTimeFormatter : ITimeFormatter
{
    private static readonly string[] UnderTwenty =
    {
        "dim", "un", "dau", "tri", "pedwar", "pump", "chwech", "saith", "wyth", "naw",
        "deg", "un ar ddeg", "deuddeg", "tri ar ddeg", "pedwar ar ddeg", "pymtheg",
        "un ar bymtheg", "dau ar bymtheg", "deunaw", "pedwar ar bymtheg",
    };

    private static readonly string[] Tens =
    {
        "dim", "deg", "ugain", "tri deg", "pedwar deg", "pum deg",
    };

    public string Format(DateTime localTime, TimeFormatOptions options)
    {
        if (options.Use24Hour)
        {
            return Exact24(localTime);
        }

        return options.Natural ? Natural(localTime) : Exact12(localTime);
    }

    private static string Natural(DateTime time)
    {
        var hour = time.Hour;
        var minute = time.Minute;

        if (minute == 0)
        {
            return hour switch
            {
                0 => "Mae'n hanner nos.",
                12 => "Mae'n hanner dydd.",
                _ => $"Mae'n {MutateFirst(Hour12(hour))} o'r gloch.",
            };
        }

        var hourWords = Hour12(hour);
        var next = NextHourWords((hour + 1) % 24);

        return minute switch
        {
            15 => $"Mae'n chwarter wedi {hourWords}.",
            30 => $"Mae'n hanner awr wedi {hourWords}.",
            45 => $"Mae'n chwarter i {next}.",
            < 30 => $"Mae'n {MutateFirst(MinuteWords(minute))} wedi {hourWords}.",
            _ => $"Mae'n {MutateFirst(MinuteWords(60 - minute))} i {next}.",
        };
    }

    private static string Exact12(DateTime time)
    {
        var hourWords = MutateFirst(Hour12(time.Hour));

        return time.Minute == 0
            ? $"Mae'n {hourWords} o'r gloch."
            : $"Mae'n {hourWords} o'r gloch {MinuteWords(time.Minute)}.";
    }

    private static string Exact24(DateTime time)
    {
        if (time.Hour == 0)
        {
            return time.Minute == 0
                ? "Mae'n hanner nos."
                : $"Mae'n {MinuteWords(time.Minute)} wedi hanner nos.";
        }

        var hourWords = MutateFirst(NumberToWords(time.Hour));

        return time.Minute == 0
            ? $"Mae'n {hourWords} o'r gloch."
            : $"Mae'n {hourWords} o'r gloch a {AspirateFirst(MinuteWords(time.Minute))}.";
    }

    /// <summary>The twelve-hour clock word, with no mutation applied.</summary>
    private static string Hour12(int hour)
    {
        var twelve = hour % 12;
        return twelve == 0 ? "deuddeg" : NumberToWords(twelve);
    }

    /// <summary>
    /// The coming hour in a "quarter to" phrase: midnight and noon are named, and
    /// everything else takes the soft mutation after "i".
    /// </summary>
    private static string NextHourWords(int hour) => hour switch
    {
        0 => "hanner nos",
        12 => "hanner dydd",
        _ => MutateFirst(NumberToWords(hour % 12 == 0 ? 12 : hour % 12)),
    };

    /// <summary>
    /// "pum munud", "deng munud", "chwe munud", "dau funud": the counting word
    /// shortens or mutates before "munud".
    /// </summary>
    private static string MinuteWords(int minutes) => minutes switch
    {
        1 => "un munud",
        2 => "dau funud",
        5 => "pum munud",
        6 => "chwe munud",
        10 => "deng munud",
        _ => $"{NumberToWords(minutes)} munud",
    };

    /// <summary>Soft mutation of the first word only, as "yn" and "i" require.</summary>
    private static string MutateFirst(string phrase)
    {
        var space = phrase.IndexOf(' ');
        var first = space < 0 ? phrase : phrase[..space];
        var rest = space < 0 ? string.Empty : phrase[space..];

        var mutated = first switch
        {
            "dau" => "ddau",
            "tri" => "dri",
            "pedwar" => "bedwar",
            "pump" => "bump",
            "pum" => "bum",
            "deg" => "ddeg",
            "deng" => "ddeng",
            "deuddeg" => "ddeuddeg",
            _ => first,
        };

        return mutated + rest;
    }

    /// <summary>
    /// Aspirate mutation of the first word, which "a" (and) requires: "o'r gloch
    /// a phum munud".
    /// </summary>
    private static string AspirateFirst(string phrase)
    {
        var space = phrase.IndexOf(' ');
        var first = space < 0 ? phrase : phrase[..space];
        var rest = space < 0 ? string.Empty : phrase[space..];

        var mutated = first switch
        {
            "pum" => "phum",
            "pump" => "phump",
            "pedwar" => "phedwar",
            "tri" => "thri",
            _ => first,
        };

        return mutated + rest;
    }

    /// <summary>Spells 0-59 using the modern counting series.</summary>
    private static string NumberToWords(int value)
    {
        if (value is < 0 or > 59)
        {
            return value.ToString(CultureInfo.InvariantCulture);
        }

        if (value < 20)
        {
            return UnderTwenty[value];
        }

        var tens = value / 10;
        var ones = value % 10;

        if (tens == 2)
        {
            return ones == 0 ? "ugain" : $"{UnderTwenty[ones]} ar hugain";
        }

        return ones == 0 ? Tens[tens] : $"{Tens[tens]} {UnderTwenty[ones]}";
    }
}
