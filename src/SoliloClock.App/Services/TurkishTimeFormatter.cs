using System;
using System.Globalization;

namespace SoliloClock.Services;

/// <summary>
/// Turkish clock phrasing. Past the hour the hour takes the accusative ("dokuzu
/// beş geçiyor"); before the hour the next hour takes the dative ("ona yirmi
/// var"). Quarter and half are "çeyrek" and "buçuk".
/// </summary>
public sealed class TurkishTimeFormatter : ITimeFormatter
{
    private static readonly string[] Hours12 =
    {
        "on iki", "bir", "iki", "üç", "dört", "beş", "altı", "yedi", "sekiz", "dokuz", "on", "on bir",
    };

    /// <summary>Accusative of each hour, indexed by <c>hour % 12</c>.</summary>
    private static readonly string[] HoursAccusative =
    {
        "on ikiyi", "biri", "ikiyi", "üçü", "dördü", "beşi", "altıyı", "yediyi", "sekizi", "dokuzu", "onu", "on biri",
    };

    /// <summary>Dative of each hour, indexed by <c>hour % 12</c>.</summary>
    private static readonly string[] HoursDative =
    {
        "on ikiye", "bire", "ikiye", "üçe", "dörde", "beşe", "altıya", "yediye", "sekize", "dokuza", "ona", "on bire",
    };

    private static readonly string[] Ones =
    {
        "sıfır", "bir", "iki", "üç", "dört", "beş", "altı", "yedi", "sekiz", "dokuz",
        "on", "on bir", "on iki", "on üç", "on dört", "on beş", "on altı", "on yedi", "on sekiz", "on dokuz",
    };

    private static readonly string[] Tens = { "sıfır", "on", "yirmi", "otuz", "kırk", "elli" };

    public string Format(DateTime localTime, TimeFormatOptions options)
    {
        if (localTime.Hour == 0 && localTime.Minute == 0)
        {
            return "Gece yarısı.";
        }

        if (options.Use24Hour)
        {
            return $"Saat {Exact24(localTime.Hour, localTime.Minute)}.";
        }

        if (!options.Natural)
        {
            return $"Saat {Exact12(localTime.Hour, localTime.Minute)}.";
        }

        return Natural(localTime.Hour, localTime.Minute);
    }

    private static string Natural(int hour24, int minute)
    {
        var hour = Hours12[hour24 % 12];
        var next = hours12Slot(hour24 + 1);

        if (minute == 0)
        {
            return hour24 == 12 ? "Saat on iki." : $"Saat {hour}.";
        }

        if (minute == 15)
        {
            return $"Saat {HoursAccusative[hour24 % 12]} çeyrek geçiyor.";
        }

        if (minute == 30)
        {
            return $"Saat {hour} buçuk.";
        }

        if (minute == 45)
        {
            return $"Saat {HoursDative[next]} çeyrek var.";
        }

        return minute < 30
            ? $"Saat {HoursAccusative[hour24 % 12]} {Number(minute)} geçiyor."
            : $"Saat {HoursDative[next]} {Number(60 - minute)} var.";
    }

    private static string Exact12(int hour24, int minute)
    {
        var hour = Hours12[hour24 % 12];
        return minute == 0 ? hour : $"{hour} {Padded(minute)}";
    }

    private static string Exact24(int hour24, int minute) =>
        minute == 0 ? Number(hour24) : $"{Number(hour24)} {Padded(minute)}";

    private static int hours12Slot(int hour24) => hour24 % 12;

    /// <summary>Minutes below ten are read with "sıfır" so 9:05 is not "dokuz beş".</summary>
    private static string Padded(int minute) => minute < 10 ? $"sıfır {Number(minute)}" : Number(minute);

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
