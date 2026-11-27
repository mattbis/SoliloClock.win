using System;
using System.Globalization;

namespace SoliloClock.Services;

/// <summary>
/// Slovak clock phrasing, which counts towards the coming hour: "Je deväť
/// hodín", "Je päť minút po deviatej", "Je štvrť na desať", "Je pol deviatej",
/// "Je tri štvrte na desať" and "Je za päť minút desať".
/// </summary>
public sealed class SlovakTimeFormatter : ITimeFormatter
{
    private static readonly string[] HoursNom =
    {
        "jeden", "dva", "tri", "štyri", "päť", "šesť", "sedem", "osem", "deväť", "desať", "jedenásť", "dvanásť",
    };

    private static readonly string[] HoursAcc =
    {
        "jednu", "dve", "tri", "štyri", "päť", "šesť", "sedem", "osem", "deväť", "desať", "jedenásť", "dvanásť",
    };

    private static readonly string[] HoursLoc =
    {
        "prvej", "druhej", "tretej", "štvrtej", "piatej", "šiestej", "siedmej", "ôsmej", "deviatej", "desiatej", "jedenástej", "dvanástej",
    };

    private static readonly string[] Ones =
    {
        "nula", "jeden", "dva", "tri", "štyri", "päť", "šesť", "sedem", "osem", "deväť",
        "desať", "jedenásť", "dvanásť", "trinásť", "štrnásť", "pätnásť", "šestnásť", "sedemnásť", "osemnásť", "devätnásť",
    };

    private static readonly string[] Tens = { "nula", "desať", "dvadsať", "tridsať", "štyridsať", "päťdesiat" };

    public string Format(DateTime localTime, TimeFormatOptions options)
    {
        if (localTime.Hour == 0 && localTime.Minute == 0)
        {
            return "Je polnoc.";
        }

        if (options.Use24Hour)
        {
            var hour24 = localTime.Hour is >= 1 and <= 12 ? HourCount(localTime.Hour) : Number(localTime.Hour);
            return localTime.Minute == 0
                ? $"Je {hour24} hodín."
                : $"Je {hour24} hodín {Minutes(localTime.Minute)}.";
        }

        if (localTime.Minute == 0)
        {
            return HourSentence(localTime.Hour);
        }

        if (!options.Natural)
        {
            var hour = localTime.Hour == 0 ? 12 : localTime.Hour;
            return $"Je {HourCount(hour)} {HourNoun(hour)} {Minutes(localTime.Minute)}.";
        }

        return $"Je {Natural(localTime.Hour, localTime.Minute)}.";
    }

    private static string HourSentence(int hour24) => hour24 switch
    {
        0 => "Je polnoc.",
        12 => "Je poludnie.",
        _ => $"{Verb(hour24)} {HourCount(hour24)} {HourNoun(hour24)}.",
    };

    private static string Natural(int hour24, int minute)
    {
        var next = (hour24 + 1) % 24;

        return minute switch
        {
            15 => $"štvrť na {HoursAcc[Slot(next)]}",
            30 => $"pol {HoursLoc[Slot(next)]}",
            45 => $"tri štvrte na {HoursAcc[Slot(next)]}",
            < 30 => $"{Minutes(minute)} po {HoursLoc[Slot(hour24)]}",
            _ => $"za {MinutesTo(60 - minute)} {HoursAcc[Slot(next)]}",
        };
    }

    private static int Slot(int hour) => ((hour - 1) % 12 + 12) % 12;

    private static string Verb(int hour) => hour is 2 or 3 or 4 ? "Sú" : "Je";

    private static string HourCount(int hour) => hour switch
    {
        1 => "jedna",
        2 => "dve",
        _ => HoursNom[Slot(hour)],
    };

    private static string HourNoun(int hour) => hour == 1 ? "hodina" : hour is 2 or 3 or 4 ? "hodiny" : "hodín";

    private static string Minutes(int value) => value switch
    {
        1 => "jedna minúta",
        2 => "dve minúty",
        3 => "tri minúty",
        4 => "štyri minúty",
        _ => $"{Number(value)} minút",
    };

    private static string MinutesTo(int value) => value switch
    {
        1 => "jednu minútu",
        2 => "dve minúty",
        3 => "tri minúty",
        4 => "štyri minúty",
        _ => $"{Number(value)} minút",
    };

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
