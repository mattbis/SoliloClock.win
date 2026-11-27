using System;
using System.Globalization;

namespace SoliloClock.Services;

/// <summary>
/// Malay clock phrasing: "Sekarang pukul sembilan", "pukul sembilan suku" for a
/// quarter past, "pukul sembilan setengah" for half past and "pukul sepuluh
/// kurang suku". Noon and midnight are named.
/// </summary>
public sealed class MalayTimeFormatter : ITimeFormatter
{
    private static readonly string[] Hours12 =
    {
        "dua belas", "satu", "dua", "tiga", "empat", "lima", "enam", "tujuh", "lapan", "sembilan", "sepuluh", "sebelas",
    };

    private static readonly string[] Ones =
    {
        "sifar", "satu", "dua", "tiga", "empat", "lima", "enam", "tujuh", "lapan", "sembilan",
        "sepuluh", "sebelas", "dua belas", "tiga belas", "empat belas", "lima belas", "enam belas", "tujuh belas", "lapan belas", "sembilan belas",
    };

    private static readonly string[] Tens = { "sifar", "sepuluh", "dua puluh", "tiga puluh", "empat puluh", "lima puluh" };

    public string Format(DateTime localTime, TimeFormatOptions options)
    {
        if (localTime.Hour == 0 && localTime.Minute == 0)
        {
            return "Sekarang tengah malam.";
        }

        if (options.Use24Hour)
        {
            return localTime.Minute == 0
                ? $"Sekarang pukul {Number(localTime.Hour)}."
                : $"Sekarang pukul {Number(localTime.Hour)} {Minutes(localTime.Minute)}.";
        }

        if (!options.Natural)
        {
            return localTime.Minute == 0
                ? $"Sekarang pukul {HourName(localTime.Hour)}."
                : $"Sekarang pukul {HourName(localTime.Hour)} {Minutes(localTime.Minute)}.";
        }

        var hour = HourName(localTime.Hour);
        var next = HourName((localTime.Hour + 1) % 24);

        if (localTime.Minute == 0)
        {
            return localTime.Hour == 12 ? "Sekarang tengah hari." : $"Sekarang pukul {hour}.";
        }

        return localTime.Minute switch
        {
            15 => $"Sekarang pukul {hour} suku.",
            30 => $"Sekarang pukul {hour} setengah.",
            45 => $"Sekarang pukul {next} kurang suku.",
            < 30 => $"Sekarang pukul {hour} {Minutes(localTime.Minute)}.",
            _ => $"Sekarang pukul {next} kurang {Minutes(60 - localTime.Minute)}.",
        };
    }

    private static string HourName(int hour24) => Hours12[hour24 % 12];

    private static string Minutes(int value) => value == 1 ? "satu minit" : $"{Number(value)} minit";

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
