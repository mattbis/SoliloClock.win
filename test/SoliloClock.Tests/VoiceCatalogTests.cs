using System;
using System.Collections.Generic;
using SoliloClock.Services;
using Xunit;

namespace SoliloClock.Tests;

public sealed class VoiceCatalogTests
{
    private static readonly VoiceOption[] Mixed =
    {
        new("Microsoft David Desktop", "en-US"),
        new("Microsoft Hazel Desktop", "en-GB"),
        new("Microsoft Julie Desktop", "fr-FR"),
        new("Microsoft Paulina Desktop", "es-MX"),
    };

    [Fact]
    public void PreferredFor_ChoosesAVoiceThatSpeaksTheAnnouncementLanguage()
    {
        var voice = VoiceCatalog.PreferredFor(Mixed, "fr-FR");

        Assert.NotNull(voice);
        Assert.Equal("Microsoft Julie Desktop", voice!.Name);
    }

    [Fact]
    public void PreferredFor_MatchesByLanguageNotByRegion()
    {
        var voice = VoiceCatalog.PreferredFor(Mixed, "fr-CA");

        Assert.Equal("Microsoft Julie Desktop", voice!.Name);
    }

    [Fact]
    public void PreferredFor_PrefersTheExactRegionThenTheAlphabeticallyFirst()
    {
        var voices = new[]
        {
            new VoiceOption("Zulu", "en-AU"),
            new VoiceOption("Alpha", "en-GB"),
            new VoiceOption("Bravo", "en-US"),
        };

        Assert.Equal("Alpha", VoiceCatalog.PreferredFor(voices, "en-GB")!.Name);
        Assert.Equal("Alpha", VoiceCatalog.PreferredFor(voices, "en-NZ")!.Name);
    }

    [Fact]
    public void PreferredFor_ReturnsNullWhenOnlyOtherLanguagesAreInstalled()
    {
        Assert.Null(VoiceCatalog.PreferredFor(Mixed, "de-DE"));
        Assert.False(VoiceCatalog.SpeaksLanguage(Mixed, "de-DE"));
    }

    [Fact]
    public void PreferredFor_IgnoresVoicesThatDoNotReportACulture()
    {
        var voices = new[]
        {
            new VoiceOption("Unlabelled", string.Empty),
            new VoiceOption("Labelled", "en-GB"),
        };

        Assert.Equal("Labelled", VoiceCatalog.PreferredFor(voices, "en-GB")!.Name);
        Assert.Null(VoiceCatalog.PreferredFor(new[] { new VoiceOption("Unlabelled", " ") }, "en-GB"));
    }

    [Fact]
    public void PreferredFor_HandlesNoVoicesAtAll()
    {
        Assert.Null(VoiceCatalog.PreferredFor(Array.Empty<VoiceOption>(), "en-GB"));
        Assert.Null(VoiceCatalog.PreferredFor(null!, "en-GB"));
        Assert.False(VoiceCatalog.SpeaksLanguage(Array.Empty<VoiceOption>(), "en-GB"));
    }

    [Fact]
    public void PreferredFor_TreatsAnUnsetLocaleAsAutomaticEnglish()
    {
        // A null announcement locale resolves to the fallback catalog language.
        Assert.Equal("Microsoft Hazel Desktop", VoiceCatalog.PreferredFor(Mixed, null)!.Name);
    }

    [Fact]
    public void Speaks_IsLanguageLevelAndRejectsBlankCultures()
    {
        Assert.True(VoiceCatalog.Speaks(new VoiceOption("Julie", "fr-CA"), "fr-FR"));
        Assert.False(VoiceCatalog.Speaks(new VoiceOption("Julie", "fr-CA"), "es-ES"));
        Assert.False(VoiceCatalog.Speaks(new VoiceOption("Mystery", ""), ""));
    }
}
