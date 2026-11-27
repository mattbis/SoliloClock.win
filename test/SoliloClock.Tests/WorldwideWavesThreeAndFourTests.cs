using System;
using System.IO;
using System.Linq;
using SoliloClock.Services;
using Xunit;

namespace SoliloClock.Tests;

/// <summary>
/// M2.7 waves 3 and 4 complete the Windows voice list: Slovenian, Catalan,
/// Hebrew, Indonesian, Malay, Thai, Vietnamese and Tamil. All are
/// machine-drafted, so each ships provisionally with a complete catalog,
/// phrase pack and grammar. Hebrew rejoins Arabic as a right-to-left locale.
/// </summary>
public sealed class WorldwideWavesThreeAndFourTests
{
    private static string PhraseDirectory => Path.Combine(AppContext.BaseDirectory, "Phrases");

    [Theory]
    [InlineData("sl-SI", "SlovenianTimeFormatter")]
    [InlineData("ca-ES", "CatalanTimeFormatter")]
    [InlineData("he-IL", "HebrewTimeFormatter")]
    [InlineData("id-ID", "IndonesianTimeFormatter")]
    [InlineData("ms-MY", "MalayTimeFormatter")]
    [InlineData("th-TH", "ThaiTimeFormatter")]
    [InlineData("vi-VN", "VietnameseTimeFormatter")]
    [InlineData("ta-IN", "TamilTimeFormatter")]
    public void EachLanguage_ShipsACompleteProvisionalPack(string locale, string formatterType)
    {
        var ui = LocalizationCatalog.Load(LocalizationCatalog.DefaultDirectory, locale);

        Assert.False(ui.UsedFallback);
        Assert.Empty(ui.Diagnostics);
        Assert.Equal(locale, ui.Locale);

        var registered = LocaleRegistry.Find(locale);
        Assert.NotNull(registered);
        Assert.True(registered!.IsShipped);
        Assert.True(registered.HasPhraseCatalog);
        Assert.True(registered.IsProvisional);

        var selection = TimeFormatterFactory.Resolve(locale);
        Assert.Equal(formatterType, selection.Formatter.GetType().Name);
        Assert.False(selection.UsedFallback);

        var phrases = PhraseCatalog.Load(PhraseDirectory, locale);
        var english = PhraseCatalog.Load(PhraseDirectory, "en-GB");

        Assert.False(phrases.UsedFallback);
        Assert.Empty(phrases.Diagnostics);
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
    [InlineData("sl", "sl-SI")]
    [InlineData("ca", "ca-ES")]
    [InlineData("he", "he-IL")]
    [InlineData("id", "id-ID")]
    [InlineData("ms", "ms-MY")]
    [InlineData("th", "th-TH")]
    [InlineData("vi", "vi-VN")]
    [InlineData("ta", "ta-IN")]
    public void AutomaticLanguage_UsesTheShippedLocaleForTheSameLanguage(string system, string expected)
    {
        var choice = LocaleResolver.ResolveUi(null, system);

        Assert.Equal(expected, choice.Locale);
        Assert.False(choice.LanguageDiffers);
    }

    [Theory]
    [InlineData(9, 0, "Zdaj je devet.")]
    [InlineData(12, 0, "Zdaj je poldne.")]
    [InlineData(0, 0, "Zdaj je polnoč.")]
    [InlineData(9, 5, "Zdaj je devet in pet minut.")]
    [InlineData(9, 15, "Zdaj je devet in petnajst.")]
    [InlineData(9, 30, "Zdaj je devet in pol.")]
    [InlineData(9, 40, "Zdaj je dvajset minut do deset.")]
    [InlineData(9, 45, "Zdaj je petnajst do deset.")]
    public void Slovenian_CountsInAndToAndUsesOnesFirstTens(int hour, int minute, string expected)
    {
        var formatter = new SlovenianTimeFormatter();

        Assert.Equal(expected, formatter.Format(
            new DateTime(2026, 11, 26, hour, minute, 0),
            new TimeFormatOptions(true, false)));
    }

    [Fact]
    public void Slovenian_Reads24HourAndExactTimeInWords()
    {
        var formatter = new SlovenianTimeFormatter();

        Assert.Equal(
            "Zdaj je devetnajst petinštirideset.",
            formatter.Format(new DateTime(2026, 11, 26, 19, 45, 0), new TimeFormatOptions(true, true)));
        Assert.Equal(
            "Zdaj je devet in pet minut.",
            formatter.Format(new DateTime(2026, 11, 26, 9, 5, 0), new TimeFormatOptions(false, false)));
    }

    [Theory]
    [InlineData(9, 0, "Són les nou.")]
    [InlineData(1, 0, "És la una.")]
    [InlineData(12, 0, "És migdia.")]
    [InlineData(0, 0, "És mitjanit.")]
    [InlineData(9, 5, "Són les nou i cinc.")]
    [InlineData(9, 15, "Són les nou i quart.")]
    [InlineData(9, 30, "Són les nou i mitja.")]
    [InlineData(9, 40, "Són les deu menys vint.")]
    [InlineData(9, 45, "Són les deu menys quart.")]
    public void Catalan_UsesMenysForTheComingHourAndSingularOne(int hour, int minute, string expected)
    {
        var formatter = new CatalanTimeFormatter();

        Assert.Equal(expected, formatter.Format(
            new DateTime(2026, 11, 26, hour, minute, 0),
            new TimeFormatOptions(true, false)));
    }

    [Fact]
    public void Catalan_Reads24HourInWords()
    {
        var formatter = new CatalanTimeFormatter();

        Assert.Equal(
            "Són les dinou i quaranta-cinc.",
            formatter.Format(new DateTime(2026, 11, 26, 19, 45, 0), new TimeFormatOptions(true, true)));
        Assert.Equal(
            "Són les nou i cinc.",
            formatter.Format(new DateTime(2026, 11, 26, 9, 5, 0), new TimeFormatOptions(false, false)));
    }

    [Theory]
    [InlineData(9, 0, "השעה תשע.")]
    [InlineData(12, 0, "השעה צהריים.")]
    [InlineData(0, 0, "השעה חצות.")]
    [InlineData(9, 5, "השעה תשע וחמש דקות.")]
    [InlineData(9, 15, "השעה תשע ורבע.")]
    [InlineData(9, 30, "השעה תשע וחצי.")]
    [InlineData(9, 40, "השעה עשר פחות עשרים דקות.")]
    [InlineData(9, 45, "השעה עשר פחות רבע.")]
    public void Hebrew_UsesFeminineHoursAndPachotForTheComingHour(int hour, int minute, string expected)
    {
        var formatter = new HebrewTimeFormatter();

        Assert.Equal(expected, formatter.Format(
            new DateTime(2026, 11, 26, hour, minute, 0),
            new TimeFormatOptions(true, false)));
    }

    [Fact]
    public void Hebrew_Reads24HourInWords()
    {
        var formatter = new HebrewTimeFormatter();

        Assert.Equal(
            "השעה תשע עשרה וארבעים וחמש דקות.",
            formatter.Format(new DateTime(2026, 11, 26, 19, 45, 0), new TimeFormatOptions(true, true)));
    }

    [Theory]
    [InlineData(9, 0, "Sekarang pukul sembilan.")]
    [InlineData(12, 0, "Sekarang tengah hari.")]
    [InlineData(0, 0, "Sekarang tengah malam.")]
    [InlineData(9, 5, "Sekarang pukul sembilan lebih lima menit.")]
    [InlineData(9, 15, "Sekarang pukul sembilan lebih seperempat.")]
    [InlineData(9, 30, "Sekarang pukul setengah sepuluh.")]
    [InlineData(9, 40, "Sekarang pukul sepuluh kurang dua puluh menit.")]
    [InlineData(9, 45, "Sekarang pukul sepuluh kurang seperempat.")]
    public void Indonesian_UsesLebihAndKurangWithSetengahForHalfPast(int hour, int minute, string expected)
    {
        var formatter = new IndonesianTimeFormatter();

        Assert.Equal(expected, formatter.Format(
            new DateTime(2026, 11, 26, hour, minute, 0),
            new TimeFormatOptions(true, false)));
    }

    [Fact]
    public void Indonesian_Reads24HourInWords()
    {
        var formatter = new IndonesianTimeFormatter();

        Assert.Equal(
            "Sekarang pukul sembilan belas lebih empat puluh lima menit.",
            formatter.Format(new DateTime(2026, 11, 26, 19, 45, 0), new TimeFormatOptions(true, true)));
    }

    [Theory]
    [InlineData(9, 0, "Sekarang pukul sembilan.")]
    [InlineData(12, 0, "Sekarang tengah hari.")]
    [InlineData(0, 0, "Sekarang tengah malam.")]
    [InlineData(9, 5, "Sekarang pukul sembilan lima minit.")]
    [InlineData(9, 15, "Sekarang pukul sembilan suku.")]
    [InlineData(9, 30, "Sekarang pukul sembilan setengah.")]
    [InlineData(9, 40, "Sekarang pukul sepuluh kurang dua puluh minit.")]
    [InlineData(9, 45, "Sekarang pukul sepuluh kurang suku.")]
    public void Malay_UsesSukuAndKurang(int hour, int minute, string expected)
    {
        var formatter = new MalayTimeFormatter();

        Assert.Equal(expected, formatter.Format(
            new DateTime(2026, 11, 26, hour, minute, 0),
            new TimeFormatOptions(true, false)));
    }

    [Fact]
    public void Malay_Reads24HourInWords()
    {
        var formatter = new MalayTimeFormatter();

        Assert.Equal(
            "Sekarang pukul sembilan belas empat puluh lima minit.",
            formatter.Format(new DateTime(2026, 11, 26, 19, 45, 0), new TimeFormatOptions(true, true)));
    }

    [Theory]
    [InlineData(9, 0, "ตอนนี้เก้าโมง")]
    [InlineData(9, 5, "ตอนนี้เก้าโมงห้านาที")]
    [InlineData(9, 15, "ตอนนี้เก้าโมงสิบห้านาที")]
    [InlineData(9, 30, "ตอนนี้เก้าโมงครึ่ง")]
    [InlineData(12, 0, "ตอนนี้เที่ยงวัน")]
    [InlineData(0, 0, "ตอนนี้เที่ยงคืน")]
    public void Thai_SaysMoongAndKhrung(int hour, int minute, string expected)
    {
        var formatter = new ThaiTimeFormatter();

        Assert.Equal(expected, formatter.Format(
            new DateTime(2026, 11, 26, hour, minute, 0),
            new TimeFormatOptions(true, false)));
    }

    [Theory]
    [InlineData(19, 45, "ตอนนี้สิบเก้านาฬิกาสี่สิบห้านาที")]
    [InlineData(9, 5, "ตอนนี้เก้านาฬิกาห้านาที")]
    public void Thai_Reads24HourWithNalika(int hour, int minute, string expected)
    {
        var formatter = new ThaiTimeFormatter();

        Assert.Equal(expected, formatter.Format(
            new DateTime(2026, 11, 26, hour, minute, 0),
            new TimeFormatOptions(true, true)));
    }

    [Theory]
    [InlineData(9, 0, "Bây giờ là chín giờ.")]
    [InlineData(12, 0, "Bây giờ là giữa trưa.")]
    [InlineData(0, 0, "Bây giờ là nửa đêm.")]
    [InlineData(9, 5, "Bây giờ là chín giờ năm phút.")]
    [InlineData(9, 15, "Bây giờ là chín giờ mười lăm phút.")]
    [InlineData(9, 30, "Bây giờ là chín giờ rưỡi.")]
    [InlineData(9, 40, "Bây giờ là mười giờ kém hai mươi phút.")]
    [InlineData(9, 45, "Bây giờ là mười giờ kém mười lăm phút.")]
    public void Vietnamese_NamesTheComingHourWithKem(int hour, int minute, string expected)
    {
        var formatter = new VietnameseTimeFormatter();

        Assert.Equal(expected, formatter.Format(
            new DateTime(2026, 11, 26, hour, minute, 0),
            new TimeFormatOptions(true, false)));
    }

    [Fact]
    public void Vietnamese_Reads24HourInWords()
    {
        var formatter = new VietnameseTimeFormatter();

        Assert.Equal(
            "Bây giờ là mười chín giờ bốn mươi lăm phút.",
            formatter.Format(new DateTime(2026, 11, 26, 19, 45, 0), new TimeFormatOptions(true, true)));
    }

    [Theory]
    [InlineData(9, 0, "இப்போது ஒன்பது மணி.")]
    [InlineData(9, 5, "இப்போது ஒன்பது மணி ஐந்து நிமிடம்.")]
    [InlineData(9, 15, "இப்போது ஒன்பது மணி பதினைந்து நிமிடம்.")]
    [InlineData(9, 30, "இப்போது ஒன்பது மணி முப்பது நிமிடம்.")]
    [InlineData(9, 40, "இப்போது பத்து மணிக்கு இருபது நிமிடம்.")]
    [InlineData(9, 45, "இப்போது பத்து மணிக்கு பதினைந்து நிமிடம்.")]
    [InlineData(12, 0, "இப்போது நண்பகல்.")]
    [InlineData(0, 0, "இப்போது நள்ளிரவு.")]
    public void Tamil_UsesMaanikkuForMinutesToTheHour(int hour, int minute, string expected)
    {
        var formatter = new TamilTimeFormatter();

        Assert.Equal(expected, formatter.Format(
            new DateTime(2026, 11, 26, hour, minute, 0),
            new TimeFormatOptions(true, false)));
    }

    [Fact]
    public void Tamil_Reads24HourInWords()
    {
        var formatter = new TamilTimeFormatter();

        Assert.Equal(
            "இப்போது பத்தொன்பது மணி நாற்பத்தைந்து நிமிடம்.",
            formatter.Format(new DateTime(2026, 11, 26, 19, 45, 0), new TimeFormatOptions(true, true)));
    }

    [Theory]
    [InlineData("sl-SI")]
    [InlineData("ca-ES")]
    [InlineData("he-IL")]
    [InlineData("id-ID")]
    [InlineData("ms-MY")]
    [InlineData("th-TH")]
    [InlineData("vi-VN")]
    [InlineData("ta-IN")]
    public void EveryMinuteOfTheDay_ProducesASentenceInAllThreeModes(string locale)
    {
        var formatter = TimeFormatterFactory.Resolve(locale).Formatter;

        foreach (var options in new[]
                 {
                     new TimeFormatOptions(true, false),
                     new TimeFormatOptions(false, false),
                     new TimeFormatOptions(true, true),
                 })
        {
            for (var minuteOfDay = 0; minuteOfDay < 24 * 60; minuteOfDay++)
            {
                var time = new DateTime(2026, 11, 26, minuteOfDay / 60, minuteOfDay % 60, 0);
                var spoken = formatter.Format(time, options);

                Assert.False(string.IsNullOrWhiteSpace(spoken));
                Assert.DoesNotContain(spoken, char.IsAsciiDigit);
            }
        }
    }

    [Fact]
    public void Hebrew_IsMarkedRightToLeft()
    {
        var catalogue = LocalizationCatalog.CreateUiText("he-IL");

        Assert.True(catalogue.IsRightToLeft);
    }
}
