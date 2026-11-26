using System;
using System.Collections.Generic;

namespace SoliloClock.Services;

/// <summary>
/// Answers what the installed SAPI voices can speak. Windows voices report a
/// BCP-47 culture, so matching is by language rather than by guessing from the
/// voice name; the announcing voice is chosen the same way.
/// </summary>
public static class VoiceCatalog
{
    /// <summary>
    /// The installed voice to use when none is pinned: an exact culture match
    /// first, then any voice speaking the same language, then nothing so the
    /// caller falls back to the Windows default.
    /// </summary>
    public static VoiceOption? PreferredFor(IReadOnlyList<VoiceOption> voices, string? locale)
    {
        if (voices is null || voices.Count == 0)
        {
            return null;
        }

        var language = LocaleRegistry.LanguageOf(locale);
        var exact = LocaleRegistry.NormalizeTag(locale);

        VoiceOption? sameLanguage = null;

        foreach (var voice in voices)
        {
            if (!Speaks(voice, locale))
            {
                continue;
            }

            if (string.Equals(voice.Culture, exact, StringComparison.OrdinalIgnoreCase))
            {
                return voice;
            }

            if (sameLanguage is null ||
                string.Compare(voice.Name, sameLanguage.Name, StringComparison.CurrentCulture) < 0)
            {
                sameLanguage = voice;
            }
        }

        return sameLanguage;
    }

    /// <summary>True when this voice reports the given language (any region).</summary>
    public static bool Speaks(VoiceOption voice, string? locale)
    {
        if (voice is null || string.IsNullOrWhiteSpace(voice.Culture))
        {
            return false;
        }

        return string.Equals(
            LocaleRegistry.LanguageOf(voice.Culture),
            LocaleRegistry.LanguageOf(locale),
            StringComparison.OrdinalIgnoreCase);
    }

    public static bool SpeaksLanguage(IReadOnlyList<VoiceOption> voices, string? locale) =>
        PreferredFor(voices, locale) is not null;
}
