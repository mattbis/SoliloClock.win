using System;
using System.Globalization;

namespace SoliloClock.Services;

/// <summary>
/// Greek clock phrasing: "Είναι εννιά η ώρα", "εννιά και τέταρτο", "εννιά και
/// μισή", "δέκα παρά τέταρτο", "εννιά και δέκα", "δέκα παρά πέντε". The hour
/// keeps the feminine form ("μία", "τρεις", "τέσσερις"), midnight and noon are
/// named, and minutes are neuter ("ένα λεπτό", "πέντε λεπτά").
/// </summary>
public sealed class GreekTimeFormatter : ITimeFormatter
{
    /// <summary>1-12 as spoken hours, feminine, indexed by <c>hour % 12</c>.</summary>
    private static readonly string[] Hours12 =
    {
        "δώδεκα", "μία", "δύο", "τρεις", "τέσσερις", "πέντε",
        "έξι", "επτά", "οκτώ", "εννέα", "δέκα", "έντεκα",
    };

    private static readonly string[] Ones =
    {
        "μηδέν", "ένα", "δύο", "τρία", "τέσσερα", "πέντε", "έξι", "επτά", "οκτώ",
        "εννέα", "δέκα", "έντεκα", "δώδεκα", "δεκατρία", "δεκατέσσερα", "δεκαπέντε",
        "δεκαέξι", "δεκαεπτά", "δεκαοκτώ", "δεκαεννέα",
    };

    private static readonly string[] Tens =
    {
        "μηδέν", "δέκα", "είκοσι", "τριάντα", "σαράντα", "πενήντα",
    };

    public string Format(DateTime localTime, TimeFormatOptions options)
    {
        if (options.Use24Hour)
        {
            return $"Είναι {Exact24(localTime.Hour, localTime.Minute)}.";
        }

        if (!options.Natural)
        {
            return $"Είναι {Exact12(localTime.Hour, localTime.Minute)}.";
        }

        return $"Είναι {Natural(localTime.Hour, localTime.Minute)}.";
    }

    private static string Natural(int hour24, int minute)
    {
        if (minute == 0)
        {
            return hour24 switch
            {
                0 => "μεσάνυχτα",
                12 => "μεσημέρι",
                _ => $"{HourName(hour24)} η ώρα",
            };
        }

        var hour = HourName(hour24);
        var next = HourName((hour24 + 1) % 24);

        return minute switch
        {
            15 => $"{hour} και τέταρτο",
            30 => $"{hour} και μισή",
            45 => $"{next} παρά τέταρτο",
            < 30 => $"{hour} και {Minutes(minute)}",
            _ => $"{next} παρά {Minutes(60 - minute)}",
        };
    }

    private static string Exact12(int hour24, int minute) =>
        minute == 0
            ? $"{HourName(hour24)} ακριβώς"
            : $"{HourName(hour24)} και {Minutes(minute)}";

    private static string Exact24(int hour24, int minute)
    {
        if (hour24 == 0 && minute == 0)
        {
            return "μεσάνυχτα";
        }

        var hour = Number(hour24);
        return minute == 0
            ? $"{hour} ακριβώς"
            : $"{hour} και {Minutes(minute)}";
    }

    private static string HourName(int hour24) => Hours12[hour24 % 12];

    /// <summary>"ένα λεπτό" for one, otherwise the bare number ("πέντε λεπτά").</summary>
    private static string Minutes(int value) => value == 1
        ? "ένα λεπτό"
        : Number(value);

    /// <summary>Spells 0-59; Greek numbers above twenty are two words.</summary>
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
