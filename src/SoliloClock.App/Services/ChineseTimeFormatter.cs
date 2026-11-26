using System;
using System.Globalization;

namespace SoliloClock.Services;

/// <summary>
/// Chinese clock phrasing: "现在是九点", "九点一刻", "九点半", "差一刻十点",
/// "九点零五分". Hours and minutes are numerals, never spelled digits, and the
/// only difference between Simplified and Traditional is the character set
/// ("现在是九点" versus "現在是九點"), so one formatter serves both locales.
/// </summary>
public sealed class ChineseTimeFormatter : ITimeFormatter
{
    private static readonly string[] Digits =
    {
        "零", "一", "二", "三", "四", "五", "六", "七", "八", "九",
    };

    private readonly bool _traditional;

    public ChineseTimeFormatter(bool traditional = false) => _traditional = traditional;

    private string Point => _traditional ? "點" : "点";

    private string Now => _traditional ? "現在是" : "现在是";

    public string Format(DateTime localTime, TimeFormatOptions options)
    {
        var minute = localTime.Minute;

        if (options.Use24Hour)
        {
            return minute == 0
                ? $"{Now}{Number(localTime.Hour)}{Point}。"
                : $"{Now}{Number(localTime.Hour)}{Point}{Minutes(minute, padZero: false)}分。";
        }

        var hour = localTime.Hour % 12;
        hour = hour == 0 ? 12 : hour;

        if (!options.Natural)
        {
            return minute == 0
                ? $"{Now}{Number(hour)}{Point}整。"
                : $"{Now}{Number(hour)}{Point}{Minutes(minute, padZero: true)}分。";
        }

        if (minute == 0)
        {
            if (localTime.Hour == 0)
            {
                return $"{Now}零{Point}。";
            }

            if (localTime.Hour == 12)
            {
                return $"{Now}中午十二{Point}。";
            }

            return $"{Now}{Number(hour)}{Point}。";
        }

        return minute switch
        {
            15 => $"{Now}{Number(hour)}{Point}一刻。",
            30 => $"{Now}{Number(hour)}{Point}半。",
            45 => $"{Now}差一刻{Number(hour == 12 ? 1 : hour + 1)}{Point}。",
            < 10 => $"{Now}{Number(hour)}{Point}零{Number(minute)}分。",
            _ => $"{Now}{Number(hour)}{Point}{Number(minute)}分。",
        };
    }

    private static string Minutes(int value, bool padZero) =>
        padZero && value < 10 ? "零" + Number(value) : Number(value);

    /// <summary>Spells 0-59 with Chinese numerals (19 → 十九, 45 → 四十五).</summary>
    public static string Number(int value)
    {
        if (value is < 0 or > 59)
        {
            return value.ToString(CultureInfo.InvariantCulture);
        }

        if (value < 10)
        {
            return Digits[value];
        }

        if (value < 20)
        {
            return value == 10 ? "十" : "十" + Digits[value % 10];
        }

        var tens = value / 10;
        var ones = value % 10;
        return ones == 0
            ? Digits[tens] + "十"
            : Digits[tens] + "十" + Digits[ones];
    }
}
