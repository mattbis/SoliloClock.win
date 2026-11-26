using System;
using System.Globalization;

namespace SoliloClock.Services;

/// <summary>
/// Hindi clock phrasing: "अभी नौ बजे हैं", "सवा नौ", "साढ़े नौ", "पौने दस",
/// "नौ बजकर पाँच मिनट", and "दस बजने में दस मिनट" after the three-quarter mark.
/// Hindi hour words are the native numerals, and the numbers are spoken rather
/// than left as digits so the announcement is read out exactly once.
/// </summary>
public sealed class HindiTimeFormatter : ITimeFormatter
{
    /// <summary>1-12 as spoken hours, indexed by <c>hour % 12</c>.</summary>
    private static readonly string[] Hours =
    {
        "बारह", "एक", "दो", "तीन", "चार", "पाँच",
        "छह", "सात", "आठ", "नौ", "दस", "ग्यारह",
    };

    private static readonly string[] Numbers =
    {
        "शून्य", "एक", "दो", "तीन", "चार", "पाँच", "छह", "सात", "आठ", "नौ",
        "दस", "ग्यारह", "बारह", "तेरह", "चौदह", "पंद्रह", "सोलह", "सत्रह",
        "अठारह", "उन्नीस", "बीस", "इक्कीस", "बाईस", "तेईस", "चौबीस", "पच्चीस",
        "छब्बीस", "सत्ताईस", "अट्ठाईस", "उनतीस", "तीस", "इकतीस", "बत्तीस",
        "तैंतीस", "चौंतीस", "पैंतीस", "छत्तीस", "सैंतीस", "अड़तीस", "उनतालीस",
        "चालीस", "इकतालीस", "बयालीस", "तैंतालीस", "चवालीस", "पैंतालीस",
        "छियालीस", "सैंतालीस", "अड़तालीस", "उनचास", "पचास", "इक्यावन", "बावन",
        "तिरपन", "चौवन", "पचपन", "छप्पन", "सत्तावन", "अट्ठावन", "उनसठ",
    };

    public string Format(DateTime localTime, TimeFormatOptions options)
    {
        var minute = localTime.Minute;

        if (options.Use24Hour)
        {
            return minute == 0
                ? $"अभी {Number(localTime.Hour)} बजे हैं।"
                : $"अभी {Number(localTime.Hour)} बजकर {Number(minute)} मिनट हैं।";
        }

        var hour = localTime.Hour % 12;
        hour = hour == 0 ? 12 : hour;

        if (!options.Natural)
        {
            return minute == 0
                ? $"अभी {Number(hour)} बजे हैं।"
                : $"अभी {Number(hour)} बजकर {TwoDigit(minute)} मिनट हैं।";
        }

        if (minute == 0)
        {
            if (localTime.Hour == 0)
            {
                return "अभी आधी रात है।";
            }

            if (localTime.Hour == 12)
            {
                return "अभी दोपहर है।";
            }

            return $"अभी {Number(hour)} बजे हैं।";
        }

        var next = hour == 12 ? 1 : hour + 1;

        return minute switch
        {
            15 => $"अभी सवा {Number(hour)} बजे हैं।",
            30 => $"अभी साढ़े {Number(hour)} बजे हैं।",
            45 => $"अभी पौने {Number(next)} बजे हैं।",
            < 45 => $"अभी {Number(hour)} बजकर {Number(minute)} मिनट हैं।",
            _ => $"अभी {Number(next)} बजने में {Number(60 - minute)} मिनट हैं।",
        };
    }

    private static string TwoDigit(int value) =>
        value < 10 ? "शून्य " + Number(value) : Number(value);

    /// <summary>Spells 0-59 with Hindi numerals.</summary>
    public static string Number(int value)
    {
        if (value is < 0 or > 59)
        {
            return value.ToString(CultureInfo.InvariantCulture);
        }

        return Numbers[value];
    }
}
