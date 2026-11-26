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
    [InlineData("de-DE")]
    [InlineData("ar")]
    [InlineData("ja-JP")]
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
