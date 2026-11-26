using System;
using System.IO;
using System.Linq;
using SoliloClock.Services;
using Xunit;

namespace SoliloClock.Tests;

public sealed class GermanCatalogTests
{
    private static string PhraseDirectory => Path.Combine(AppContext.BaseDirectory, "Phrases");

    [Fact]
    public void GermanUiCatalog_LoadsWithoutFallbackAndCoversEveryKey()
    {
        var result = LocalizationCatalog.Load(LocalizationCatalog.DefaultDirectory, "de-DE");

        Assert.False(result.UsedFallback);
        Assert.Empty(result.Diagnostics);
        Assert.Equal("de-DE", result.Locale);
        Assert.Equal("Jetzt sprechen", result.Strings[LocalizationKeys.ActionSpeakNow]);
        Assert.Equal("Deutsch", LocaleRegistry.GetDisplayName("de-DE"));
    }

    [Fact]
    public void GermanUiCatalog_UsesGermanQuotesWithoutStrayAsciiQuotes()
    {
        var result = LocalizationCatalog.Load(LocalizationCatalog.DefaultDirectory, "de-DE");

        Assert.Contains("„", result.Strings[LocalizationKeys.StatusSavedVoiceMissing]);
        Assert.Contains("“", result.Strings[LocalizationKeys.StatusSavedVoiceMissing]);

        foreach (var pair in result.Strings)
        {
            Assert.DoesNotContain("  ", pair.Value);
            Assert.DoesNotContain("'", pair.Value);
        }
    }

    [Fact]
    public void GermanPhraseCatalog_IsCompleteAndKeepsTheReviewedMetadata()
    {
        var german = PhraseCatalog.Load(PhraseDirectory, "de-DE");
        var english = PhraseCatalog.Load(PhraseDirectory, "en-GB");

        Assert.False(german.UsedFallback);
        Assert.Empty(german.Diagnostics);
        Assert.Equal(100, german.Phrases.Count);
        Assert.Equal(english.Phrases.Count, german.Phrases.Count);

        var englishById = english.Phrases.ToDictionary(
            phrase => phrase.Id,
            phrase => phrase.Text,
            StringComparer.OrdinalIgnoreCase);

        foreach (var phrase in german.Phrases)
        {
            Assert.True(englishById.ContainsKey(phrase.Id), $"unexpected id {phrase.Id}");
            Assert.NotEqual(englishById[phrase.Id], phrase.Text);
            Assert.False(string.IsNullOrWhiteSpace(phrase.Text));
            Assert.True(phrase.FamilyFriendly);
        }

        foreach (var pair in english.Phrases.Zip(german.Phrases))
        {
            Assert.Equal(pair.First.Category, pair.Second.Category);
            Assert.Equal(pair.First.Tone, pair.Second.Tone);
            Assert.Equal(pair.First.Weight, pair.Second.Weight);
            Assert.Equal(pair.First.TimeWindow, pair.Second.TimeWindow);
            Assert.Equal(pair.First.MinActiveMinutes, pair.Second.MinActiveMinutes);
        }
    }

    [Fact]
    public void GermanStartup_WiresCatalogCultureAndGrammarTogether()
    {
        var text = LocalizationCatalog.CreateUiText("de-DE");
        var selection = TimeFormatterFactory.Resolve(text.Locale);

        Assert.False(text.UsedFallback);
        Assert.Equal("de-DE", text.Locale);
        Assert.Equal("de-DE", text.Culture.Name);
        Assert.False(text.IsRightToLeft);
        Assert.IsType<GermanTimeFormatter>(selection.Formatter);

        var spoken = selection.Formatter.Format(
            new DateTime(2026, 11, 26, 19, 45, 0),
            new TimeFormatOptions(Natural: true, Use24Hour: false));

        Assert.Equal("Es ist Viertel vor acht.", spoken);
    }

    [Fact]
    public void GermanIsRegisteredAsAShippedReviewedLocale()
    {
        var german = LocaleRegistry.Find("de-DE");

        Assert.NotNull(german);
        Assert.True(german!.IsShipped);
        Assert.True(german.HasTimeGrammar);
        Assert.True(german.HasPhraseCatalog);
        Assert.False(german.IsProvisional);
    }

    [Fact]
    public void AutomaticLanguage_ChoosesGermanForGermanSpeakingWindows()
    {
        Assert.Equal("de-DE", LocaleResolver.ResolveUi(null, "de-DE").Locale);
        Assert.Equal("de-DE", LocaleResolver.ResolveUi(null, "de-AT").Locale);
        Assert.False(LocaleResolver.ResolveUi(null, "de-DE").LanguageDiffers);
    }
}
