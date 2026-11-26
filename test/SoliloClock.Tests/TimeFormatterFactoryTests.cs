using SoliloClock.Services;
using Xunit;

namespace SoliloClock.Tests;

public sealed class TimeFormatterFactoryTests
{
    [Theory]
    [InlineData("en-GB")]
    [InlineData("en-US")]
    [InlineData("en-AU")]
    [InlineData("en-CA")]
    public void EnglishLocales_UseEnglishGrammarWithoutFallback(string locale)
    {
        var selection = TimeFormatterFactory.Resolve(locale);

        Assert.IsType<EnglishTimeFormatter>(selection.Formatter);
        Assert.False(selection.UsedFallback);
        Assert.Null(selection.Diagnostic);
    }

    [Theory]
    [InlineData("fr-FR")]
    [InlineData("fr-CA")]
    public void FrenchLocales_UseFrenchGrammar(string locale)
    {
        var selection = TimeFormatterFactory.Resolve(locale);

        Assert.IsType<FrenchTimeFormatter>(selection.Formatter);
        Assert.False(selection.UsedFallback);
        Assert.Null(selection.Diagnostic);
    }

    [Theory]
    [InlineData("cy-GB")]
    [InlineData("cy")]
    public void WelshLocales_UseWelshGrammar(string locale)
    {
        var selection = TimeFormatterFactory.Resolve(locale);

        Assert.IsType<WelshTimeFormatter>(selection.Formatter);
        Assert.False(selection.UsedFallback);
        Assert.Null(selection.Diagnostic);
    }

    [Theory]
    [InlineData("de-DE")]
    [InlineData("de-AT")]
    public void GermanLocales_UseGermanGrammar(string locale)
    {
        var selection = TimeFormatterFactory.Resolve(locale);

        Assert.IsType<GermanTimeFormatter>(selection.Formatter);
        Assert.False(selection.UsedFallback);
        Assert.Null(selection.Diagnostic);
    }

    [Theory]
    [InlineData("ar")]
    [InlineData("ja-JP")]
    [InlineData("pl-PL")]
    public void LocalesWithoutGrammar_FallBackToEnglishAndSaySo(string locale)
    {
        var selection = TimeFormatterFactory.Resolve(locale);

        Assert.IsType<EnglishTimeFormatter>(selection.Formatter);
        Assert.True(selection.UsedFallback);
        Assert.Equal(locale, selection.Locale);
        Assert.False(string.IsNullOrWhiteSpace(selection.Diagnostic));
    }

    [Fact]
    public void EmptyLocale_UsesTheEnglishFallback()
    {
        var selection = TimeFormatterFactory.Resolve(null);

        Assert.Equal("en-GB", selection.Locale);
        Assert.False(selection.UsedFallback);
    }

    [Fact]
    public void EveryLocaleClaimingGrammar_HasARegisteredFormatter()
    {
        foreach (var locale in LocaleRegistry.All)
        {
            if (!locale.HasTimeGrammar)
            {
                continue;
            }

            Assert.True(
                TimeFormatterFactory.HasGrammarFor(locale.Tag),
                $"{locale.Tag} claims time grammar but has no formatter");
        }
    }
}
