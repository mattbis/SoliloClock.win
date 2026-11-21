using System;

namespace SoliloClock.Models;

/// <summary>
/// User settings persisted as JSON under %APPDATA%\SoliloClock\settings.json.
/// Unknown JSON fields are ignored; invalid values are normalized on load.
/// </summary>
public sealed class AppSettings
{
    public int IntervalMinutes { get; set; } = 15;

    /// <summary>
    /// Wall-clock moment at which the 45-minute cadence was selected. 45-minute
    /// announcements are every 45 elapsed minutes from this anchor, not aligned
    /// to the hour.
    /// </summary>
    public DateTimeOffset? Anchor45 { get; set; }

    public bool Muted { get; set; }

    public string Theme { get; set; } = "pink";

    public double WindowOpacity { get; set; } = 0.92;

    public bool AlwaysOnTop { get; set; }

    public bool ReducedMotion { get; set; }

    public bool Use24Hour { get; set; }

    public bool UseNaturalTime { get; set; } = true;

    /// <summary>Percentage of scheduled announcements that may include a joke.</summary>
    public int JokePercent { get; set; } = 25;

    public string Tone { get; set; } = "cheeky";

    public bool FamilyFriendly { get; set; } = true;

    public string UiLanguage { get; set; } = "en-GB";

    /// <summary>Installed SAPI voice name; null means the system default voice.</summary>
    public string? SpeechVoice { get; set; }

    public int SpeechRate { get; set; }

    public int SpeechVolume { get; set; } = 100;

    public bool CloseToTrayExplained { get; set; }

    public DateTimeOffset? LastSpokenAt { get; set; }
}
