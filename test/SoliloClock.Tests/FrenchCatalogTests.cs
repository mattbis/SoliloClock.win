using System;
using System.IO;
using System.Linq;
using SoliloClock.Services;
using Xunit;

namespace SoliloClock.Tests;

public sealed class FrenchCatalogTests
{
    private static string PhraseDirectory => Path.Combine(AppContext.BaseDirectory, "Phrases");

    [Fact]
    public void FrenchUiCatalog_LoadsWithoutFallbackAndCoversEveryKey()
    {
        var result = LocalizationCatalog.Load(LocalizationCatalog.DefaultDirectory, "fr-FR");

        Assert.False(result.UsedFallback);
        Assert.Empty(result.Diagnostics);
        Assert.Equal("fr-FR", result.Locale);
        Assert.Equal("Parler maintenant", result.Strings[LocalizationKeys.ActionSpeakNow]);
        Assert.Equal("Français", LocaleRegistry.GetDisplayName("fr-FR"));
    }

    [Fact]
    public void FrenchUiCatalog_UsesFrenchTypography()
    {
        var result = LocalizationCatalog.Load(LocalizationCatalog.DefaultDirectory, "fr-FR");

        foreach (var pair in result.Strings)
        {
            foreach (var mark in new[] { ':', ';', '?', '!' })
            {
                var index = pair.Value.IndexOf(mark);
                while (index > 0)
                {
                    Assert.True(
                        pair.Value[index - 1] == '\u00A0',
                        $"{pair.Key}: a non-breaking space must precede '{mark}'");
                    index = pair.Value.IndexOf(mark, index + 1);
                }
            }

            var open = pair.Value.IndexOf('«');
            if (open < 0)
            {
                continue;
            }

            var close = pair.Value.IndexOf('»', open + 1);
            Assert.True(close > open, $"{pair.Key}: unbalanced guillemets");
            Assert.Equal('\u00A0', pair.Value[open + 1]);
            Assert.Equal('\u00A0', pair.Value[close - 1]);
        }
    }

    [Fact]
    public void FrenchPhraseCatalog_IsCompleteAndNotACopyOfEnglish()
    {
        var french = PhraseCatalog.Load(PhraseDirectory, "fr-FR");
        var english = PhraseCatalog.Load(PhraseDirectory, "en-GB");

        Assert.False(french.UsedFallback);
        Assert.Empty(french.Diagnostics);
        Assert.Equal(100, french.Phrases.Count);
        Assert.Equal(english.Phrases.Count, french.Phrases.Count);

        var englishById = english.Phrases.ToDictionary(
            phrase => phrase.Id,
            phrase => phrase.Text,
            StringComparer.OrdinalIgnoreCase);

        foreach (var phrase in french.Phrases)
        {
            Assert.True(englishById.ContainsKey(phrase.Id), $"unexpected id {phrase.Id}");
            Assert.NotEqual(englishById[phrase.Id], phrase.Text);
            Assert.False(string.IsNullOrWhiteSpace(phrase.Text));
            Assert.True(phrase.FamilyFriendly);
        }

        Assert.Equal(
            english.Phrases.Select(phrase => phrase.Id).OrderBy(id => id, StringComparer.Ordinal),
            french.Phrases.Select(phrase => phrase.Id).OrderBy(id => id, StringComparer.Ordinal));
    }

    [Fact]
    public void FrenchPhraseCatalog_KeepsTheReviewedMetadata()
    {
        var french = PhraseCatalog.Load(PhraseDirectory, "fr-FR");
        var english = PhraseCatalog.Load(PhraseDirectory, "en-GB");

        foreach (var pair in english.Phrases.Zip(french.Phrases))
        {
            Assert.Equal(pair.First.Category, pair.Second.Category);
            Assert.Equal(pair.First.Tone, pair.Second.Tone);
            Assert.Equal(pair.First.Weight, pair.Second.Weight);
            Assert.Equal(pair.First.TimeWindow, pair.Second.TimeWindow);
            Assert.Equal(pair.First.MinActiveMinutes, pair.Second.MinActiveMinutes);
        }
    }

    [Fact]
    public void FrenchStartup_WiresCatalogCultureAndGrammarTogether()
    {
        var text = LocalizationCatalog.CreateUiText("fr-FR");
        var selection = TimeFormatterFactory.Resolve(text.Locale);

        Assert.False(text.UsedFallback);
        Assert.Equal("fr-FR", text.Locale);
        Assert.Equal("fr-FR", text.Culture.Name);
        Assert.Equal("Parler maintenant", text[LocalizationKeys.ActionSpeakNow]);
        Assert.False(text.IsRightToLeft);
        Assert.IsType<FrenchTimeFormatter>(selection.Formatter);

        var spoken = selection.Formatter.Format(
            new DateTime(2026, 11, 26, 19, 45, 0),
            new TimeFormatOptions(Natural: true, Use24Hour: false));

        Assert.Equal("Il est huit heures moins le quart du soir.", spoken);
    }

    [Fact]
    public void FrenchIsRegisteredAsAShippedLocale()
    {
        var french = LocaleRegistry.Find("fr-FR");

        Assert.NotNull(french);
        Assert.True(french!.IsShipped);
        Assert.True(french.HasTimeGrammar);
        Assert.True(french.HasPhraseCatalog);
    }
}
