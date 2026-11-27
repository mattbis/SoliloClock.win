using System;
using System.Globalization;

namespace SoliloClock.Services;

/// <summary>
/// Vietnamese clock phrasing: "Bây giờ là chín giờ", "chín giờ rưỡi" for half
/// past, "mười giờ kém mười lăm phút" for a quarter to. Noon and midnight are
/// named, and 21/25 take the shortened "mốt"/"lăm" forms.
/// </summary>
public sealed class VietnameseTimeFormatter : ITimeFormatter
{
    private static readonly string[] Hours12 =
    {
        "mười hai", "một", "hai", "ba", "bốn", "năm", "sáu", "bảy", "tám", "chín", "mười", "mười một",
    };

    private static readonly string[] Ones =
    {
        "không", "một", "hai", "ba", "bốn", "năm", "sáu", "bảy", "tám", "chín",
        "mười", "mười một", "mười hai", "mười ba", "mười bốn", "mười lăm", "mười sáu", "mười bảy", "mười tám", "mười chín",
    };

    private static readonly string[] Tens = { "không", "mười", "hai mươi", "ba mươi", "bốn mươi", "năm mươi" };

    public string Format(DateTime localTime, TimeFormatOptions options)
    {
        if (localTime.Hour == 0 && localTime.Minute == 0)
        {
            return "Bây giờ là nửa đêm.";
        }

        if (options.Use24Hour)
        {
            return localTime.Minute == 0
                ? $"Bây giờ là {Number(localTime.Hour)} giờ."
                : $"Bây giờ là {Number(localTime.Hour)} giờ {Number(localTime.Minute)} phút.";
        }

        if (!options.Natural)
        {
            return localTime.Minute == 0
                ? $"Bây giờ là {HourName(localTime.Hour)} giờ."
                : $"Bây giờ là {HourName(localTime.Hour)} giờ {Number(localTime.Minute)} phút.";
        }

        var hour = HourName(localTime.Hour);
        if (localTime.Minute == 0)
        {
            return localTime.Hour == 12 ? "Bây giờ là giữa trưa." : $"Bây giờ là {hour} giờ.";
        }

        if (localTime.Minute == 30)
        {
            return $"Bây giờ là {hour} giờ rưỡi.";
        }

        var next = HourName((localTime.Hour + 1) % 24);
        return localTime.Minute < 30
            ? $"Bây giờ là {hour} giờ {Number(localTime.Minute)} phút."
            : $"Bây giờ là {next} giờ kém {Number(60 - localTime.Minute)} phút.";
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
        return ones switch
        {
            0 => Tens[tens],
            1 => $"{Tens[tens]} mốt",
            5 => $"{Tens[tens]} lăm",
            _ => $"{Tens[tens]} {Ones[ones]}",
        };
    }
}
