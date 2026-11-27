using System;
using System.IO;
using System.Linq;
using SoliloClock.Services;
using Xunit;

namespace SoliloClock.Tests;

public sealed class WelshCatalogTests
{
    private static string PhraseDirectory => Path.Combine(AppContext.BaseDirectory, "Phrases");

    [Fact]
    public void WelshUiCatalog_LoadsWithoutFallbackAndCoversEveryKey()
    {
        var result = LocalizationCatalog.Load(LocalizationCatalog.DefaultDirectory, "cy-GB");

        Assert.False(result.UsedFallback);
        Assert.Empty(result.Diagnostics);
        Assert.Equal("cy-GB", result.Locale);
        Assert.Equal("Siarad nawr", result.Strings[LocalizationKeys.ActionSpeakNow]);
        Assert.Equal("Cymraeg", LocaleRegistry.GetDisplayName("cy-GB"));
    }

    [Fact]
    public void WelshPhraseCatalog_IsCompleteAndKeepsTheReviewedMetadata()
    {
        var welsh = PhraseCatalog.Load(PhraseDirectory, "cy-GB");
        var english = PhraseCatalog.Load(PhraseDirectory, "en-GB");

        Assert.False(welsh.UsedFallback);
        Assert.Empty(welsh.Diagnostics);
        Assert.Equal(100, welsh.Phrases.Count);
        Assert.Equal(english.Phrases.Count, welsh.Phrases.Count);

        var englishById = english.Phrases.ToDictionary(
            phrase => phrase.Id,
            phrase => phrase.Text,
            StringComparer.OrdinalIgnoreCase);

        foreach (var phrase in welsh.Phrases)
        {
            Assert.True(englishById.ContainsKey(phrase.Id), $"unexpected id {phrase.Id}");
            Assert.NotEqual(englishById[phrase.Id], phrase.Text);
            Assert.False(string.IsNullOrWhiteSpace(phrase.Text));
            Assert.True(phrase.FamilyFriendly);
        }

        foreach (var pair in english.Phrases.Zip(welsh.Phrases))
        {
            Assert.Equal(pair.First.Category, pair.Second.Category);
            Assert.Equal(pair.First.Tone, pair.Second.Tone);
            Assert.Equal(pair.First.Weight, pair.Second.Weight);
            Assert.Equal(pair.First.TimeWindow, pair.Second.TimeWindow);
            Assert.Equal(pair.First.MinActiveMinutes, pair.Second.MinActiveMinutes);
        }
    }

    [Fact]
    public void WelshStartup_WiresCatalogCultureAndGrammarTogether()
    {
        var text = LocalizationCatalog.CreateUiText("cy-GB");
        var selection = TimeFormatterFactory.Resolve(text.Locale);

        Assert.False(text.UsedFallback);
        Assert.Equal("cy-GB", text.Locale);
        Assert.Equal("cy-GB", text.Culture.Name);
        Assert.Equal("Siarad nawr", text[LocalizationKeys.ActionSpeakNow]);
        Assert.False(text.IsRightToLeft);
        Assert.IsType<WelshTimeFormatter>(selection.Formatter);

        var spoken = selection.Formatter.Format(
            new DateTime(2026, 11, 26, 19, 45, 0),
            new TimeFormatOptions(Natural: true, Use24Hour: false));

        Assert.Equal("Mae'n chwarter i wyth.", spoken);
    }

    [Fact]
    public void WelshIsRegisteredAsAShippedButProvisionalLocale()
    {
        var welsh = LocaleRegistry.Find("cy-GB");

        Assert.NotNull(welsh);
        Assert.True(welsh!.IsShipped);
        Assert.True(welsh.HasTimeGrammar);
        Assert.True(welsh.HasPhraseCatalog);
        Assert.True(welsh.IsProvisional);
    }

    [Fact]
    public void OnlyDraftLocales_AreFlaggedProvisional()
    {
        var provisional = LocaleRegistry.All.Where(locale => locale.IsProvisional).ToList();

        Assert.Equal(
            new[]
            {
                "cy-GB", "da-DK", "sv-SE", "nb-NO", "cs-CZ",
                "tr-TR", "hu-HU", "ro-RO", "hr-HR", "bg-BG", "sk-SK", "fi-FI",
                "sl-SI", "ca-ES", "id-ID", "ms-MY",
                "ar", "he-IL", "hi", "ta-IN", "th-TH", "vi-VN",
                "zh-Hans", "zh-Hant", "ja-JP", "ko-KR",
            },
            provisional.Select(locale => locale.Tag));
        Assert.All(provisional, locale =>
        {
            Assert.True(locale.IsShipped);
            Assert.True(locale.HasPhraseCatalog);
        });
    }

    [Fact]
    public void AutomaticLanguage_ChoosesWelshForAWelshWindows()
    {
        var choice = LocaleResolver.ResolveUi(null, "cy-GB");

        Assert.Equal("cy-GB", choice.Locale);
        Assert.False(choice.LanguageDiffers);
    }
}
