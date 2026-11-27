using System;
using System.Globalization;

namespace SoliloClock.Services;

/// <summary>
/// Hebrew clock phrasing: "השעה תשע", "השעה תשע ורבע", "השעה תשע וחצי",
/// "השעה עשר פחות רבע". Hours use the feminine forms, minutes are "דקות", and
/// midnight and noon are named. The window mirrors through the culture's
/// right-to-left reading order.
/// </summary>
public sealed class HebrewTimeFormatter : ITimeFormatter
{
    private static readonly string[] Hours12 =
    {
        "שתים עשרה", "אחת", "שתיים", "שלוש", "ארבע", "חמש", "שש", "שבע", "שמונה", "תשע", "עשר", "אחת עשרה",
    };

    private static readonly string[] Ones =
    {
        "אפס", "אחת", "שתיים", "שלוש", "ארבע", "חמש", "שש", "שבע", "שמונה", "תשע",
        "עשר", "אחת עשרה", "שתים עשרה", "שלוש עשרה", "ארבע עשרה", "חמש עשרה", "שש עשרה", "שבע עשרה", "שמונה עשרה", "תשע עשרה",
    };

    private static readonly string[] Tens = { "אפס", "עשר", "עשרים", "שלושים", "ארבעים", "חמישים" };

    public string Format(DateTime localTime, TimeFormatOptions options)
    {
        if (localTime.Hour == 0 && localTime.Minute == 0)
        {
            return "השעה חצות.";
        }

        if (options.Use24Hour)
        {
            return localTime.Minute == 0
                ? $"השעה {Number(localTime.Hour)}."
                : $"השעה {Number(localTime.Hour)} ו{Minutes(localTime.Minute)}.";
        }

        if (!options.Natural)
        {
            return localTime.Minute == 0
                ? $"השעה {HourName(localTime.Hour)}."
                : $"השעה {HourName(localTime.Hour)} ו{Minutes(localTime.Minute)}.";
        }

        var hour = HourName(localTime.Hour);
        var next = HourName((localTime.Hour + 1) % 24);

        if (localTime.Minute == 0)
        {
            return localTime.Hour == 12 ? "השעה צהריים." : $"השעה {hour}.";
        }

        return localTime.Minute switch
        {
            15 => $"השעה {hour} ורבע.",
            30 => $"השעה {hour} וחצי.",
            45 => $"השעה {next} פחות רבע.",
            < 30 => $"השעה {hour} ו{Minutes(localTime.Minute)}.",
            _ => $"השעה {next} פחות {Minutes(60 - localTime.Minute)}.",
        };
    }

    private static string HourName(int hour24) => Hours12[hour24 % 12];

    private static string Minutes(int value) => value switch
    {
        1 => "דקה",
        2 => "שתי דקות",
        _ => $"{Number(value)} דקות",
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
        return ones == 0 ? Tens[tens] : $"{Tens[tens]} ו{Ones[ones]}";
    }
}
