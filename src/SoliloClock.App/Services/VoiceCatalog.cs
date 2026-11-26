using System;
using System.Collections.Generic;

namespace SoliloClock.Services;

/// <summary>
/// Answers whether the installed SAPI voices can speak a language. Windows
/// voices report a BCP-47 culture, so matching is by language rather than by
/// guessing from the voice name.
/// </summary>
public static class VoiceCatalog
{
    public static bool SpeaksLanguage(IReadOnlyList<VoiceOption> voices, string? locale)
    {
        if (voices is null || voices.Count == 0)
        {
            return false;
        }

        var language = LocaleRegistry.LanguageOf(locale);

        foreach (var voice in voices)
        {
            if (string.IsNullOrWhiteSpace(voice.Culture))
            {
                continue;
            }

            if (string.Equals(
                    LocaleRegistry.LanguageOf(voice.Culture),
                    language,
                    StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        return false;
    }
}
