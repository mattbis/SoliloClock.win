using System;

namespace SoliloClock.Models;

/// <summary>
/// Content tone presets. Higher values are progressively edgier, so a
/// "Polite" setting only allows polite lines, while "Unhinged" allows all.
/// </summary>
public enum ContentTone
{
    Polite = 0,
    Cheeky = 1,
    Unhinged = 2,
}

public static class ContentTones
{
    public static bool TryParse(string? value, out ContentTone tone)
    {
        switch (value?.Trim().ToLowerInvariant())
        {
            case "polite":
                tone = ContentTone.Polite;
                return true;
            case "cheeky":
                tone = ContentTone.Cheeky;
                return true;
            case "unhinged":
                tone = ContentTone.Unhinged;
                return true;
            default:
                tone = ContentTone.Cheeky;
                return false;
        }
    }

    public static string ToName(ContentTone tone) => tone switch
    {
        ContentTone.Polite => "polite",
        ContentTone.Cheeky => "cheeky",
        ContentTone.Unhinged => "unhinged",
        _ => throw new ArgumentOutOfRangeException(nameof(tone)),
    };
}
