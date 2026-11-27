using System;
using System.Globalization;

namespace SoliloClock.Services;

/// <summary>
/// Bulgarian clock phrasing: "Сега е девет часът", "девет и петнадесет", "девет
/// и половина", "без петнадесет десет". Minutes are feminine ("една минута",
/// "две минути").
/// </summary>
public sealed class BulgarianTimeFormatter : ITimeFormatter
{
    private static readonly string[] Hours12 =
    {
        "дванадесет", "един", "два", "три", "четири", "пет", "шест", "седем", "осем", "девет", "десет", "единадесет",
    };

    private static readonly string[] Ones =
    {
        "нула", "едно", "две", "три", "четири", "пет", "шест", "седем", "осем", "девет",
        "десет", "единадесет", "дванадесет", "тринадесет", "четиринадесет", "петнадесет", "шестнадесет", "седемнадесет", "осемнадесет", "деветнадесет",
    };

    private static readonly string[] Tens = { "нула", "десет", "двадесет", "тридесет", "четиридесет", "петдесет" };

    public string Format(DateTime localTime, TimeFormatOptions options)
    {
        if (localTime.Hour == 0 && localTime.Minute == 0)
        {
            return "Сега е полунощ.";
        }

        if (options.Use24Hour)
        {
            var hour24 = Number(localTime.Hour);
            return localTime.Minute == 0
                ? $"Сега е {hour24} часа."
                : $"Сега е {hour24} часа и {Minutes(localTime.Minute)}.";
        }

        if (!options.Natural)
        {
            var exactHour = $"{HourName(localTime.Hour)} часът";
            return localTime.Minute == 0
                ? $"Сега е {exactHour}."
                : $"Сега е {exactHour} и {Minutes(localTime.Minute)}.";
        }

        return Natural(localTime.Hour, localTime.Minute);
    }

    private static string Natural(int hour24, int minute)
    {
        var hour = HourName(hour24);
        var next = HourName((hour24 + 1) % 24);

        if (minute == 0)
        {
            return hour24 == 12 ? "Сега е обед." : $"Сега е {hour} часът.";
        }

        return minute switch
        {
            15 => $"Сега е {hour} и петнадесет.",
            30 => $"Сега е {hour} и половина.",
            45 => $"Сега е без петнадесет {next}.",
            < 30 => $"Сега е {hour} и {Minutes(minute)}.",
            _ => $"Сега е без {Minutes(60 - minute)} {next}.",
        };
    }

    private static string HourName(int hour24) => Hours12[hour24 % 12];

    private static string Minutes(int value) =>
        value == 1 ? "една минута" : $"{Number(value)} минути";

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
        return ones == 0 ? Tens[tens] : $"{Tens[tens]} и {Ones[ones]}";
    }
}
