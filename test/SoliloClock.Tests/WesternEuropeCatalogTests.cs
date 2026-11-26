using System;
using System.IO;
using System.Linq;
using SoliloClock.Services;
using Xunit;

namespace SoliloClock.Tests;

/// <summary>
/// The whole of M2.2 shares one acceptance check: every shipped language has a
/// complete UI catalog, 100 phrases with the reviewed metadata and its own time
/// grammar, and automatic language resolution finds it.
/// </summary>
public sealed class WesternEuropeCatalogTests
{
    private static string PhraseDirectory => Path.Combine(AppContext.BaseDirectory, "Phrases");

    [Theory]
    [InlineData("es-ES", "SpanishTimeFormatter")]
    [InlineData("it-IT", "ItalianTimeFormatter")]
    [InlineData("nl-NL", "DutchTimeFormatter")]
    [InlineData("pt-PT", "PortugueseTimeFormatter")]
    public void EachLanguage_ShipsACompletePack(string locale, string formatterType)
    {
        var ui = LocalizationCatalog.Load(LocalizationCatalog.DefaultDirectory, locale);

        Assert.False(ui.UsedFallback);
        Assert.Empty(ui.Diagnostics);
        Assert.Equal(locale, ui.Locale);

        var registered = LocaleRegistry.Find(locale);
        Assert.NotNull(registered);
        Assert.True(registered!.IsShipped);
        Assert.True(registered.HasPhraseCatalog);
        Assert.False(registered.IsProvisional);

        var selection = TimeFormatterFactory.Resolve(locale);
        Assert.Equal(formatterType, selection.Formatter.GetType().Name);
        Assert.False(selection.UsedFallback);

        var phrases = PhraseCatalog.Load(PhraseDirectory, locale);
        var english = PhraseCatalog.Load(PhraseDirectory, "en-GB");

        Assert.False(phrases.UsedFallback);
        Assert.Empty(phrases.Diagnostics);
        Assert.Equal(english.Phrases.Count, phrases.Phrases.Count);
        Assert.Equal(100, phrases.Phrases.Count);

        var englishById = english.Phrases.ToDictionary(
            phrase => phrase.Id,
            phrase => phrase.Text,
            StringComparer.OrdinalIgnoreCase);

        foreach (var phrase in phrases.Phrases)
        {
            Assert.True(englishById.ContainsKey(phrase.Id), $"{locale}: unexpected id {phrase.Id}");
            Assert.NotEqual(englishById[phrase.Id], phrase.Text);
            Assert.False(string.IsNullOrWhiteSpace(phrase.Text));
        }

        foreach (var pair in english.Phrases.Zip(phrases.Phrases))
        {
            Assert.Equal(pair.First.Id, pair.Second.Id);
            Assert.Equal(pair.First.Category, pair.Second.Category);
            Assert.Equal(pair.First.Tone, pair.Second.Tone);
            Assert.Equal(pair.First.Weight, pair.Second.Weight);
            Assert.Equal(pair.First.TimeWindow, pair.Second.TimeWindow);
            Assert.Equal(pair.First.MinActiveMinutes, pair.Second.MinActiveMinutes);
        }
    }

    [Theory]
    [InlineData("es-MX", "es-ES")]
    [InlineData("it-CH", "it-IT")]
    [InlineData("nl-BE", "nl-NL")]
    [InlineData("pt-BR", "pt-PT")]
    public void AutomaticLanguage_UsesTheShippedLocaleForTheSameLanguage(string system, string expected)
    {
        var choice = LocaleResolver.ResolveUi(null, system);

        Assert.Equal(expected, choice.Locale);
        Assert.False(choice.LanguageDiffers);
    }
}
