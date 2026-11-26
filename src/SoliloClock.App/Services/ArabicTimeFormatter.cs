using System;
using System.Globalization;

namespace SoliloClock.Services;

/// <summary>
/// Arabic clock phrasing in Modern Standard Arabic: "الساعة التاسعة",
/// "التاسعة والربع", "التاسعة والنصف", "العاشرة إلا ربعًا", "التاسعة وخمس
/// دقائق". Hour names are feminine ordinals, and minute nouns take Arabic
/// agreement: "دقيقة واحدة", the dual "دقيقتين", the plural "ثلاث دقائق" for
/// three to ten, and the singular "خمس عشرة دقيقة" above ten.
/// </summary>
public sealed class ArabicTimeFormatter : ITimeFormatter
{
    /// <summary>1-12, indexed by <c>hour % 12</c>.</summary>
    private static readonly string[] Hours =
    {
        "الثانية عشرة", "الواحدة", "الثانية", "الثالثة", "الرابعة", "الخامسة",
        "السادسة", "السابعة", "الثامنة", "التاسعة", "العاشرة", "الحادية عشرة",
    };

    /// <summary>Every 24-hour value as a feminine ordinal.</summary>
    private static readonly string[] Hours24 =
    {
        "الثانية عشرة", "الواحدة", "الثانية", "الثالثة", "الرابعة", "الخامسة",
        "السادسة", "السابعة", "الثامنة", "التاسعة", "العاشرة", "الحادية عشرة",
        "الثانية عشرة", "الثالثة عشرة", "الرابعة عشرة", "الخامسة عشرة",
        "السادسة عشرة", "السابعة عشرة", "الثامنة عشرة", "التاسعة عشرة",
        "العشرين", "الحادية والعشرين", "الثانية والعشرين", "الثالثة والعشرين",
    };

    private static readonly string[] Ones =
    {
        "صفر", "واحد", "اثنان", "ثلاثة", "أربعة", "خمسة", "ستة", "سبعة", "ثمانية",
        "تسعة", "عشرة", "أحد عشر", "اثنا عشر", "ثلاثة عشر", "أربعة عشر",
        "خمسة عشر", "ستة عشر", "سبعة عشر", "ثمانية عشر", "تسعة عشر",
    };

    private static readonly string[] Tens =
    {
        "صفر", "عشرة", "عشرون", "ثلاثون", "أربعون", "خمسون",
    };

    private static readonly string[] TensGenitive =
    {
        "صفر", "عشرة", "عشرين", "ثلاثين", "أربعين", "خمسين",
    };

    /// <summary>Feminine numerals used with دقائق ("خمس دقائق").</summary>
    private static readonly string[] FeminineDigits =
    {
        "صفر", "واحدة", "اثنتان", "ثلاث", "أربع", "خمس", "ست", "سبع", "ثمان", "تسع", "عشر",
    };

    private static readonly string[] FeminineTeens =
    {
        "عشر", "إحدى عشرة", "اثنتا عشرة", "ثلاث عشرة", "أربع عشرة", "خمس عشرة",
        "ست عشرة", "سبع عشرة", "ثمان عشرة", "تسع عشرة",
    };

    public string Format(DateTime localTime, TimeFormatOptions options)
    {
        var hour = localTime.Hour;
        var minute = localTime.Minute;

        if (options.Use24Hour)
        {
            return Exact24(hour, minute);
        }

        if (!options.Natural)
        {
            return Exact12(hour, minute);
        }

        if (minute == 0)
        {
            if (hour == 0)
            {
                return "منتصف الليل.";
            }

            if (hour == 12)
            {
                return "الظهر.";
            }
        }

        return $"الساعة {Natural(hour, minute)}.";
    }

    private static string Natural(int hour24, int minute)
    {
        var hour = Hours[hour24 % 12];
        var next = Hours[(hour24 + 1) % 12];

        return minute switch
        {
            0 => hour,
            15 => $"{hour} والربع",
            30 => $"{hour} والنصف",
            45 => $"{next} إلا ربعًا",
            < 30 => $"{hour} و{Minutes(minute, genitive: false)}",
            _ => $"{next} إلا {Minutes(60 - minute, genitive: true)}",
        };
    }

    private static string Exact12(int hour24, int minute)
    {
        var hour = Hours[hour24 % 12];
        return minute == 0
            ? $"الساعة {hour}."
            : $"الساعة {hour} و{Minutes(minute, genitive: false)}.";
    }

    private static string Exact24(int hour24, int minute)
    {
        if (hour24 == 0 && minute == 0)
        {
            return "منتصف الليل.";
        }

        var hour = Hours24[hour24];
        return minute == 0
            ? $"الساعة {hour}."
            : $"الساعة {hour} و{Minutes(minute, genitive: false)}.";
    }

    /// <summary>
    /// Minute count with Arabic number agreement. "إلا" governs the genitive, so
    /// the tens change there (عشرين rather than عشرون).
    /// </summary>
    private static string Minutes(int value, bool genitive) => value switch
    {
        1 => "دقيقة واحدة",
        2 => "دقيقتين",
        >= 3 and <= 10 => $"{FeminineDigits[value]} دقائق",
        <= 19 => $"{FeminineTeens[value - 10]} دقيقة",
        _ => $"{Number(value, genitive)} دقيقة",
    };

    /// <summary>Spells 0-59, joining the ones to the tens with "و".</summary>
    public static string Number(int value, bool genitive = false)
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
        var tensWord = genitive ? TensGenitive[tens] : Tens[tens];
        return ones == 0 ? tensWord : $"{Ones[ones]} و{tensWord}";
    }
}
