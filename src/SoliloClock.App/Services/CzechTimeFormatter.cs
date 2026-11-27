using System;
using System.Globalization;

namespace SoliloClock.Services;

/// <summary>
/// Czech clock phrasing, which counts towards the coming hour: "Je devět hodin",
/// "Je pět minut po deváté", "Je čtvrt na deset", "Je půl desáté", "Je tři
/// čtvrtě na deset" and "Je za pět minut deset". Hours and minutes therefore
/// need nominative, accusative, locative and genitive forms, plus the
/// hodina/hodiny/hodin agreement.
/// </summary>
public sealed class CzechTimeFormatter : ITimeFormatter
{
    /// <summary>1-12 nominative, used for "Je devět hodin".</summary>
    private static readonly string[] HoursNom =
    {
        "jedna", "dva", "tři", "čtyři", "pět", "šest", "sedm", "osm", "devět", "deset", "jedenáct", "dvanáct",
    };

    /// <summary>1-12 accusative, used after "na" ("čtvrt na deset").</summary>
    private static readonly string[] HoursAcc =
    {
        "jednu", "dvě", "tři", "čtyři", "pět", "šest", "sedm", "osm", "devět", "deset", "jedenáct", "dvanáct",
    };

    /// <summary>1-12 locative, used after "po" ("po deváté").</summary>
    private static readonly string[] HoursLoc =
    {
        "jedné", "druhé", "třetí", "čtvrté", "páté", "šesté", "sedmé", "osmé", "deváté", "desáté", "jedenácté", "dvanácté",
    };

    private static readonly string[] Ones =
    {
        "nula", "jedna", "dva", "tři", "čtyři", "pět", "šest", "sedm", "osm", "devět",
        "deset", "jedenáct", "dvanáct", "třináct", "čtrnáct", "patnáct", "šestnáct", "sedmnáct", "osmnáct", "devatenáct",
    };

    private static readonly string[] Tens = { "nula", "deset", "dvacet", "třicet", "čtyřicet", "padesát" };

    public string Format(DateTime localTime, TimeFormatOptions options)
    {
        if (options.Use24Hour)
        {
            return $"Je {Exact24(localTime.Hour, localTime.Minute)}.";
        }

        if (localTime.Minute == 0)
        {
            return HourSentence(localTime.Hour);
        }

        if (!options.Natural)
        {
            return $"Je {Exact12(localTime.Hour, localTime.Minute)}.";
        }

        return $"Je {Natural(localTime.Hour, localTime.Minute)}.";
    }

    /// <summary>Full sentence for a whole hour, where the verb depends on the count.</summary>
    private static string HourSentence(int hour24) => hour24 switch
    {
        0 => "Je půlnoc.",
        12 => "Je poledne.",
        _ => $"{Verb(hour24)} {HourCount(hour24)} {HourNoun(hour24)}.",
    };

    private static string Natural(int hour24, int minute)
    {
        var next = Hour24(hour24 + 1);

        if (minute == 15)
        {
            return $"čtvrt na {HoursAcc[Slot(next)]}";
        }

        if (minute == 30)
        {
            return $"půl {HoursLoc[Slot(next)]}";
        }

        if (minute == 45)
        {
            return $"tři čtvrtě na {HoursAcc[Slot(next)]}";
        }

        if (minute < 30)
        {
            return $"{Minutes(minute)} po {HoursLoc[Slot(hour24)]}";
        }

        return $"za {MinutesBefore(60 - minute)} {HoursAcc[Slot(next)]}";
    }

    private static string Exact12(int hour24, int minute)
    {
        var hour = hour24 == 0 ? 12 : hour24;
        return $"{HourCount(hour)} {HourNoun(hour)} {Minutes(minute)}";
    }

    private static string Exact24(int hour24, int minute)
    {
        if (hour24 == 0 && minute == 0)
        {
            return "půlnoc";
        }

        var hourWord = hour24 is >= 1 and <= 12 ? HourCount(hour24) : Number(hour24);
        var noun = hour24 == 1 ? "hodina" : hour24 is 2 or 3 or 4 ? "hodiny" : "hodin";

        return minute == 0 ? $"{hourWord} {noun}" : $"{hourWord} {noun} {Minutes(minute)}";
    }

    private static int Hour24(int hour24) => hour24 % 24;

    /// <summary>Nominative minutes: "jedna minuta", "dvě minuty", "pět minut".</summary>
    private static string Minutes(int value) => value switch
    {
        1 => "jedna minuta",
        2 => "dvě minuty",
        3 => "tři minuty",
        4 => "čtyři minuty",
        _ => $"{Number(value)} minut",
    };

    /// <summary>Accusative minutes after "za": "za jednu minutu", "za dvě minuty".</summary>
    private static string MinutesBefore(int value) => value switch
    {
        1 => "jednu minutu",
        2 => "dvě minuty",
        3 => "tři minuty",
        4 => "čtyři minuty",
        _ => $"{Number(value)} minut",
    };

    /// <summary>Czech says "Jsou dvě hodiny" for two to four, "Je" otherwise.</summary>
    private static string Verb(int hour) => hour is 2 or 3 or 4 ? "Jsou" : "Je";

    /// <summary>Zero-based slot for an hour in the 1-12 tables, safe for hour 0.</summary>
    private static int Slot(int hour) => ((hour - 1) % 12 + 12) % 12;

    /// <summary>Counting form: Czech says "dvě hodiny", not "dva hodiny".</summary>
    private static string HourCount(int hour) => hour == 2 ? "dvě" : HoursNom[(hour - 1) % 12];

    /// <summary>"hodina", "hodiny" for two to four, "hodin" from five up.</summary>
    private static string HourNoun(int hour) => hour == 1 ? "hodina" : hour is 2 or 3 or 4 ? "hodiny" : "hodin";

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
