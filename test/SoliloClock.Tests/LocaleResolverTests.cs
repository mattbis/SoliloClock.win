using SoliloClock.Services;
using Xunit;

namespace SoliloClock.Tests;

public sealed class LocaleResolverTests
{
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("auto")]
    [InlineData("AUTO")]
    [InlineData("system")]
    [InlineData("os")]
    public void AutomaticSettings_AreRecognized(string? setting) =>
        Assert.True(LocaleResolver.IsAutomatic(setting));

    [Theory]
    [InlineData("en-GB")]
    [InlineData("fr-FR")]
    public void RealTags_AreNotAutomatic(string setting) =>
        Assert.False(LocaleResolver.IsAutomatic(setting));

    [Fact]
    public void Automatic_UsesTheWindowsLanguageWhenItShips()
    {
        var choice = LocaleResolver.ResolveUi(null, "fr-FR");

        Assert.Equal("fr-FR", choice.Locale);
        Assert.Equal("fr-FR", choice.RequestedLocale);
        Assert.True(choice.FollowsSystem);
        Assert.False(choice.LanguageDiffers);
    }

    [Fact]
    public void Automatic_UsesTheShippedLocaleForTheSameLanguage()
    {
        var choice = LocaleResolver.ResolveUi(null, "fr-CA");

        Assert.Equal("fr-FR", choice.Locale);
        Assert.Equal("fr-CA", choice.RequestedLocale);
        Assert.False(choice.LanguageDiffers);
    }

    [Fact]
    public void Automatic_StaysEnglishForLanguagesWithoutACatalog()
    {
        var choice = LocaleResolver.ResolveUi(null, "th-TH");

        Assert.Equal("en-GB", choice.Locale);
        Assert.Equal("th-TH", choice.RequestedLocale);
        Assert.True(choice.FollowsSystem);
        Assert.True(choice.LanguageDiffers);
    }

    [Fact]
    public void Automatic_HandlesAnUnknownSystemTag()
    {
        var choice = LocaleResolver.ResolveUi("auto", "not a locale");

        Assert.Equal("en-GB", choice.Locale);
        Assert.Equal("en-GB", choice.RequestedLocale);
        Assert.False(choice.LanguageDiffers);
    }

    [Fact]
    public void ExplicitOverride_BeatsTheWindowsLanguage()
    {
        var choice = LocaleResolver.ResolveUi("fr-FR", "de-DE");

        Assert.Equal("fr-FR", choice.Locale);
        Assert.Equal("fr-FR", choice.RequestedLocale);
        Assert.False(choice.FollowsSystem);
    }

    [Fact]
    public void ExplicitUnsupportedTag_IsKeptSoTheFallbackCanBeReported()
    {
        var choice = LocaleResolver.ResolveUi("th-TH", "en-GB");

        Assert.Equal("th-TH", choice.Locale);
        Assert.False(choice.FollowsSystem);
    }

    [Fact]
    public void Speech_AutomaticFollowsTheInterface()
    {
        var choice = LocaleResolver.ResolveSpeech(null, "fr-FR");

        Assert.Equal("fr-FR", choice.Locale);
        Assert.True(choice.FollowsSystem);
    }

    [Fact]
    public void Speech_CanDifferFromTheInterface()
    {
        var choice = LocaleResolver.ResolveSpeech("en-GB", "fr-FR");

        Assert.Equal("en-GB", choice.Locale);
        Assert.False(choice.FollowsSystem);
    }

    [Fact]
    public void DetectedSystemLocale_IsAUsableTag()
    {
        var detected = LocaleResolver.DetectSystemLocale();

        Assert.Equal(detected, LocaleRegistry.NormalizeTag(detected));
        Assert.True(detected.Length > 0);
    }
}
