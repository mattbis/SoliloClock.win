using System;
using System.Collections.Generic;
using System.Globalization;

namespace SoliloClock.Services;

/// <summary>
/// Tamil clock phrasing: "இப்போது ஒன்பது மணி" (nine o'clock), "ஒன்பது மணி
/// ஐந்து நிமிடம்" for minutes past, and "பத்து மணிக்கு இருபது நிமிடம்" for
/// minutes to the coming hour. Noon and midnight are named, and one o'clock
/// uses the attributive "ஒரு".
/// </summary>
public sealed class TamilTimeFormatter : ITimeFormatter
{
    private static readonly string[] Hours12 =
    {
        "பன்னிரண்டு", "ஒரு", "இரண்டு", "மூன்று", "நான்கு", "ஐந்து", "ஆறு", "ஏழு", "எட்டு", "ஒன்பது", "பத்து", "பதினொன்று",
    };

    private static readonly string[] Ones =
    {
        "பூஜ்யம்", "ஒன்று", "இரண்டு", "மூன்று", "நான்கு", "ஐந்து", "ஆறு", "ஏழு", "எட்டு", "ஒன்பது",
        "பத்து", "பதினொன்று", "பன்னிரண்டு", "பதின்மூன்று", "பதினான்கு", "பதினைந்து", "பதினாறு", "பதினேழு", "பதினெட்டு", "பத்தொன்பது",
    };

    private static readonly string[] Tens = { "பூஜ்யம்", "பத்து", "இருபது", "முப்பது", "நாற்பது", "ஐம்பது" };

    public string Format(DateTime localTime, TimeFormatOptions options)
    {
        if (localTime.Hour == 0 && localTime.Minute == 0)
        {
            return "இப்போது நள்ளிரவு.";
        }

        if (options.Use24Hour)
        {
            return localTime.Minute == 0
                ? $"இப்போது {Number(localTime.Hour)} மணி."
                : $"இப்போது {Number(localTime.Hour)} மணி {Number(localTime.Minute)} நிமிடம்.";
        }

        var hour = HourName(localTime.Hour);

        if (!options.Natural)
        {
            return localTime.Minute == 0
                ? $"இப்போது {hour} மணி."
                : $"இப்போது {hour} மணி {Number(localTime.Minute)} நிமிடம்.";
        }

        if (localTime.Minute == 0)
        {
            return localTime.Hour == 12 ? "இப்போது நண்பகல்." : $"இப்போது {hour} மணி.";
        }

        if (localTime.Minute <= 30)
        {
            return $"இப்போது {hour} மணி {Number(localTime.Minute)} நிமிடம்.";
        }

        var next = HourName((localTime.Hour + 1) % 24);
        return $"இப்போது {next} மணிக்கு {Number(60 - localTime.Minute)} நிமிடம்.";
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
        return ones == 0 ? Tens[tens] : Compound[value];
    }

    /// <summary>
    /// Tamil fuses the tens and the unit into one word from twenty-one up
    /// (இருபத்தைந்து for 25), so the forms are listed rather than assembled.
    /// </summary>
    private static readonly Dictionary<int, string> Compound = new()
    {
        [21] = "இருபத்தொன்று", [22] = "இருபத்திரண்டு", [23] = "இருபத்துமூன்று",
        [24] = "இருபத்துநான்கு", [25] = "இருபத்தைந்து", [26] = "இருபத்தாறு",
        [27] = "இருபத்தேழு", [28] = "இருபத்தெட்டு", [29] = "இருபத்தொன்பது",
        [31] = "முப்பத்தொன்று", [32] = "முப்பத்திரண்டு", [33] = "முப்பத்துமூன்று",
        [34] = "முப்பத்துநான்கு", [35] = "முப்பத்தைந்து", [36] = "முப்பத்தாறு",
        [37] = "முப்பத்தேழு", [38] = "முப்பத்தெட்டு", [39] = "முப்பத்தொன்பது",
        [41] = "நாற்பத்தொன்று", [42] = "நாற்பத்திரண்டு", [43] = "நாற்பத்துமூன்று",
        [44] = "நாற்பத்துநான்கு", [45] = "நாற்பத்தைந்து", [46] = "நாற்பத்தாறு",
        [47] = "நாற்பத்தேழு", [48] = "நாற்பத்தெட்டு", [49] = "நாற்பத்தொன்பது",
        [51] = "ஐம்பத்தொன்று", [52] = "ஐம்பத்திரண்டு", [53] = "ஐம்பத்துமூன்று",
        [54] = "ஐம்பத்துநான்கு", [55] = "ஐம்பத்தைந்து", [56] = "ஐம்பத்தாறு",
        [57] = "ஐம்பத்தேழு", [58] = "ஐம்பத்தெட்டு", [59] = "ஐம்பத்தொன்பது",
    };
}
