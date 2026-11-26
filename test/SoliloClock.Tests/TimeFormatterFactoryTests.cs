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
    [InlineData("es-ES")]
    [InlineData("es-MX")]
    public void SpanishLocales_UseSpanishGrammar(string locale)
    {
        var selection = TimeFormatterFactory.Resolve(locale);

        Assert.IsType<SpanishTimeFormatter>(selection.Formatter);
        Assert.False(selection.UsedFallback);
        Assert.Null(selection.Diagnostic);
    }

    [Theory]
    [InlineData("it-IT")]
    [InlineData("it-CH")]
    public void ItalianLocales_UseItalianGrammar(string locale)
    {
        var selection = TimeFormatterFactory.Resolve(locale);

        Assert.IsType<ItalianTimeFormatter>(selection.Formatter);
        Assert.False(selection.UsedFallback);
        Assert.Null(selection.Diagnostic);
    }

    [Theory]
    [InlineData("nl-NL")]
    [InlineData("nl-BE")]
    public void DutchLocales_UseDutchGrammar(string locale)
    {
        var selection = TimeFormatterFactory.Resolve(locale);

        Assert.IsType<DutchTimeFormatter>(selection.Formatter);
        Assert.False(selection.UsedFallback);
        Assert.Null(selection.Diagnostic);
    }

    [Theory]
    [InlineData("pt-PT")]
    [InlineData("pt-BR")]
    public void PortugueseLocales_UsePortugueseGrammar(string locale)
    {
        var selection = TimeFormatterFactory.Resolve(locale);

        Assert.IsType<PortugueseTimeFormatter>(selection.Formatter);
        Assert.False(selection.UsedFallback);
        Assert.Null(selection.Diagnostic);
    }

    [Theory]
    [InlineData("pl-PL")]
    [InlineData("pl")]
    public void PolishLocales_UsePolishGrammar(string locale)
    {
        var selection = TimeFormatterFactory.Resolve(locale);

        Assert.IsType<PolishTimeFormatter>(selection.Formatter);
        Assert.False(selection.UsedFallback);
        Assert.Null(selection.Diagnostic);
    }

    [Theory]
    [InlineData("ru-RU")]
    [InlineData("ru")]
    public void RussianLocales_UseRussianGrammar(string locale)
    {
        var selection = TimeFormatterFactory.Resolve(locale);

        Assert.IsType<RussianTimeFormatter>(selection.Formatter);
        Assert.False(selection.UsedFallback);
        Assert.Null(selection.Diagnostic);
    }

    [Theory]
    [InlineData("el-GR")]
    [InlineData("el")]
    public void GreekLocales_UseGreekGrammar(string locale)
    {
        var selection = TimeFormatterFactory.Resolve(locale);

        Assert.IsType<GreekTimeFormatter>(selection.Formatter);
        Assert.False(selection.UsedFallback);
        Assert.Null(selection.Diagnostic);
    }

    [Theory]
    [InlineData("hi")]
    [InlineData("ko-KR")]
    [InlineData("tr-TR")]
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
