using System;
using System.Globalization;

namespace SoliloClock.Services;

/// <summary>
/// Japanese clock phrasing: "今は九時です", "九時半です", "九時十五分です",
/// "十九時四十五分です". Japanese reads the hour then the minutes with no
/// past/to form, uses 半 for the half hour, names noon 正午 and midnight 零時,
/// and speaks the numerals rather than the digits.
/// </summary>
public sealed class JapaneseTimeFormatter : ITimeFormatter
{
    private static readonly string[] Digits =
    {
        "零", "一", "二", "三", "四", "五", "六", "七", "八", "九",
    };

    public string Format(DateTime localTime, TimeFormatOptions options)
    {
        var minute = localTime.Minute;

        if (options.Use24Hour)
        {
            return minute == 0
                ? $"今は{Number(localTime.Hour)}時です。"
                : $"今は{Number(localTime.Hour)}時{Number(minute)}分です。";
        }

        var hour = localTime.Hour % 12;
        hour = hour == 0 ? 12 : hour;

        if (!options.Natural)
        {
            return minute == 0
                ? $"今は{Number(hour)}時ちょうどです。"
                : minute < 10
                    ? $"今は{Number(hour)}時零{Number(minute)}分です。"
                    : $"今は{Number(hour)}時{Number(minute)}分です。";
        }

        if (minute == 0)
        {
            if (localTime.Hour == 0)
            {
                return "今は零時です。";
            }

            if (localTime.Hour == 12)
            {
                return "今は正午です。";
            }
        }

        return minute switch
        {
            0 => $"今は{Number(hour)}時です。",
            30 => $"今は{Number(hour)}時半です。",
            _ => $"今は{Number(hour)}時{Number(minute)}分です。",
        };
    }

    /// <summary>Spells 0-59 with Japanese numerals (19 → 十九, 45 → 四十五).</summary>
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
