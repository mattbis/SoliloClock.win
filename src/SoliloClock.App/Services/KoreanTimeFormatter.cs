using System;
using System.Globalization;

namespace SoliloClock.Services;

/// <summary>
/// Korean clock phrasing: "지금은 아홉 시입니다", "아홉 시 반입니다", "아홉 시
/// 십오 분입니다", "지금은 정오입니다". Hours use the native Korean numerals
/// (한 시, 두 시, 세 시) while minutes use the Sino-Korean series (십오 분,
/// 사십오 분), and Japanese-style period words name noon and midnight.
/// </summary>
public sealed class KoreanTimeFormatter : ITimeFormatter
{
    /// <summary>1-12 in native Korean, indexed by <c>hour % 12</c>.</summary>
    private static readonly string[] NativeHours =
    {
        "열두", "한", "두", "세", "네", "다섯",
        "여섯", "일곱", "여덟", "아홉", "열", "열한",
    };

    private static readonly string[] SinoDigits = { "영", "일", "이", "삼", "사", "오", "육", "칠", "팔", "구" };

    public string Format(DateTime localTime, TimeFormatOptions options)
    {
        var minute = localTime.Minute;

        if (options.Use24Hour)
        {
            return minute == 0
                ? $"지금은 {Sino(localTime.Hour)} 시입니다."
                : $"지금은 {Sino(localTime.Hour)} 시 {Sino(minute)} 분입니다.";
        }

        var hour = localTime.Hour % 12;
        hour = hour == 0 ? 12 : hour;

        if (!options.Natural)
        {
            return minute == 0
                ? $"지금은 {NativeHours[hour % 12]} 시 정각입니다."
                : $"지금은 {NativeHours[hour % 12]} 시 {TwoDigit(minute)} 분입니다.";
        }

        if (minute == 0)
        {
            if (localTime.Hour == 0)
            {
                return "지금은 자정입니다.";
            }

            if (localTime.Hour == 12)
            {
                return "지금은 정오입니다.";
            }
        }

        return minute switch
        {
            0 => $"지금은 {NativeHours[hour % 12]} 시입니다.",
            30 => $"지금은 {NativeHours[hour % 12]} 시 반입니다.",
            _ => $"지금은 {NativeHours[hour % 12]} 시 {Sino(minute)} 분입니다.",
        };
    }

    private static string TwoDigit(int value) => value < 10 ? "영" + Sino(value) : Sino(value);

    /// <summary>Spells 0-59 with Sino-Korean numerals (15 → 십오, 45 → 사십오).</summary>
    public static string Sino(int value)
    {
        if (value is < 0 or > 59)
        {
            return value.ToString(CultureInfo.InvariantCulture);
        }

        if (value < 10)
        {
            return SinoDigits[value];
        }

        if (value < 20)
        {
            return value == 10 ? "십" : "십" + SinoDigits[value % 10];
        }

        var tens = value / 10;
        var ones = value % 10;
        return ones == 0
            ? SinoDigits[tens] + "십"
            : SinoDigits[tens] + "십" + SinoDigits[ones];
    }
}
