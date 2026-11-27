using System;
using System.Globalization;

namespace SoliloClock.Services;

/// <summary>
/// Thai clock phrasing: "ตอนนี้เก้าโมงห้านาที" (nine o'clock five minutes),
/// "เก้าโมงครึ่ง" for half past, and "สิบเก้านาฬิกา" when the 24-hour clock is
/// selected. Noon and midnight are named. Thai writes without spaces between
/// words, so the sentence is assembled without them.
/// </summary>
public sealed class ThaiTimeFormatter : ITimeFormatter
{
    private static readonly string[] Hours12 =
    {
        "สิบสอง", "หนึ่ง", "สอง", "สาม", "สี่", "ห้า", "หก", "เจ็ด", "แปด", "เก้า", "สิบ", "สิบเอ็ด",
    };

    private static readonly string[] Ones =
    {
        "ศูนย์", "หนึ่ง", "สอง", "สาม", "สี่", "ห้า", "หก", "เจ็ด", "แปด", "เก้า",
        "สิบ", "สิบเอ็ด", "สิบสอง", "สิบสาม", "สิบสี่", "สิบห้า", "สิบหก", "สิบเจ็ด", "สิบแปด", "สิบเก้า",
    };

    private static readonly string[] Tens = { "ศูนย์", "สิบ", "ยี่สิบ", "สามสิบ", "สี่สิบ", "ห้าสิบ" };

    public string Format(DateTime localTime, TimeFormatOptions options)
    {
        if (localTime.Hour == 0 && localTime.Minute == 0)
        {
            return "ตอนนี้เที่ยงคืน";
        }

        if (options.Use24Hour)
        {
            return localTime.Minute == 0
                ? $"ตอนนี้{Number(localTime.Hour)}นาฬิกา"
                : $"ตอนนี้{Number(localTime.Hour)}นาฬิกา{Number(localTime.Minute)}นาที";
        }

        var hour = HourName(localTime.Hour);
        if (localTime.Minute == 0)
        {
            return localTime.Hour == 12 ? "ตอนนี้เที่ยงวัน" : $"ตอนนี้{hour}โมง";
        }

        if (!options.Natural)
        {
            return $"ตอนนี้{hour}โมง{Number(localTime.Minute)}นาที";
        }

        return localTime.Minute == 30
            ? $"ตอนนี้{hour}โมงครึ่ง"
            : $"ตอนนี้{hour}โมง{Number(localTime.Minute)}นาที";
    }

    private static string HourName(int hour24) => Hours12[hour24 % 12];

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
        return ones == 0 ? Tens[tens] : $"{Tens[tens]}{Ones[ones]}";
    }
}
