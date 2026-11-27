using System;
using System.Globalization;

namespace SoliloClock.Services;

/// <summary>
/// Swedish clock phrasing: "Klockan är nio", "fem över nio", "kvart över nio",
/// "halv tio" (half to ten) and "kvart i tio". Tens and ones join into one word
/// ("tjugofem"), and midnight and noon are named.
/// </summary>
public sealed class SwedishTimeFormatter : ITimeFormatter
{
    private static readonly string[] Hours12 =
    {
        "tolv", "ett", "två", "tre", "fyra", "fem", "sex", "sju", "åtta", "nio", "tio", "elva",
    };

    private static readonly string[] Ones =
    {
        "noll", "ett", "två", "tre", "fyra", "fem", "sex", "sju", "åtta", "nio",
        "tio", "elva", "tolv", "tretton", "fjorton", "femton", "sexton", "sjutton", "arton", "nitton",
    };

    private static readonly string[] Tens = { "noll", "tio", "tjugo", "trettio", "fyrtio", "femtio" };

    public string Format(DateTime localTime, TimeFormatOptions options)
    {
        if (localTime.Hour == 0 && localTime.Minute == 0)
        {
            return "Klockan är midnatt.";
        }

        if (options.Use24Hour)
        {
            return $"Klockan är {Exact24(localTime.Hour, localTime.Minute)}.";
        }

        if (!options.Natural)
        {
            return $"Klockan är {Exact12(localTime.Hour, localTime.Minute)}.";
        }

        return $"Klockan är {Natural(localTime.Hour, localTime.Minute)}.";
    }

    private static string Natural(int hour24, int minute)
    {
        if (minute == 0)
        {
            return hour24 == 12 ? "mitt på dagen" : HourName(hour24);
        }

        var hour = HourName(hour24);
        var next = HourName((hour24 + 1) % 24);

        return minute switch
        {
            15 => $"kvart över {hour}",
            30 => $"halv {next}",
            45 => $"kvart i {next}",
            < 30 => $"{MinutePhrase(minute)} över {hour}",
            _ => $"{MinutePhrase(60 - minute)} i {next}",
        };
    }

    private static string Exact12(int hour24, int minute) =>
        minute == 0 ? HourName(hour24) : $"{HourName(hour24)} {Padded(minute)}";

    private static string Exact24(int hour24, int minute) =>
        minute == 0 ? Number(hour24) : $"{Number(hour24)} {Padded(minute)}";

    private static string HourName(int hour24) => Hours12[hour24 % 12];

    /// <summary>Clock speech drops the word for minutes: "fem över nio".</summary>
    private static string MinutePhrase(int value) =>
        value == 1 ? "en minut" : Number(value);

    private static string Padded(int minute) => minute < 10 ? $"noll {Number(minute)}" : Number(minute);

    /// <summary>Spells 0-59; Swedish writes 25 as one word, "tjugofem".</summary>
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
