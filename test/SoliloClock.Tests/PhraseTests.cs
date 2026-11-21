using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using SoliloClock.Models;
using SoliloClock.Services;
using Xunit;

namespace SoliloClock.Tests;

public sealed class PhraseCatalogTests : IDisposable
{
    private readonly string _tempDirectory;

    public PhraseCatalogTests()
    {
        _tempDirectory = Path.Combine(
            Path.GetTempPath(),
            "soliloclock-phrases-" + Guid.NewGuid().ToString("N"));
    }

    private static string BundledDirectory => Path.Combine(AppContext.BaseDirectory, "Phrases");

    public void Dispose()
    {
        try
        {
            if (Directory.Exists(_tempDirectory))
            {
                Directory.Delete(_tempDirectory, recursive: true);
            }
        }
        catch (IOException)
        {
        }
    }

    [Fact]
    public void BundledEnglishCatalog_HasOneHundredValidUniqueEntries()
    {
        var result = PhraseCatalog.Load(BundledDirectory, "en-GB");

        Assert.True(result.Phrases.Count >= 100, $"Expected at least 100 phrases, got {result.Phrases.Count}.");
        Assert.DoesNotContain(result.Diagnostics, d => d.Contains("Skipped entry", StringComparison.OrdinalIgnoreCase));
        Assert.Equal(result.Phrases.Count, result.Phrases.Select(p => p.Id).Distinct(StringComparer.OrdinalIgnoreCase).Count());

        foreach (var phrase in result.Phrases)
        {
            Assert.False(string.IsNullOrWhiteSpace(phrase.Id));
            Assert.False(string.IsNullOrWhiteSpace(phrase.Text));
            Assert.True(phrase.Weight > 0);
        }

        Assert.Contains(result.Phrases, p => p.Category == PhraseCategory.Rare);
        Assert.Contains(result.Phrases, p => p.TimeWindow == DayPart.Morning);
        Assert.Contains(result.Phrases, p => p.MinActiveMinutes == 180);
        Assert.Contains(result.Phrases, p => p.Tone == ContentTone.Unhinged);
    }

    [Fact]
    public void MissingLocale_FallsBackToBritishEnglish()
    {
        var result = PhraseCatalog.Load(BundledDirectory, "zz-ZZ");

        Assert.True(result.UsedFallback);
        Assert.NotEmpty(result.Phrases);
        Assert.NotEmpty(result.Diagnostics);
    }

    [Fact]
    public void InvalidEntries_AreSkippedWithoutFailingTheCatalog()
    {
        Directory.CreateDirectory(_tempDirectory);
        File.WriteAllText(Path.Combine(_tempDirectory, "xx-XX.json"), """
        {
          "locale": "xx-XX",
          "phrases": [
            { "id": "ok.001", "text": "Valid line.", "category": "computer", "tone": "cheeky" },
            { "id": "ok.001", "text": "Duplicate id.", "category": "computer", "tone": "cheeky" },
            { "id": "bad.002", "text": "", "category": "computer", "tone": "cheeky" },
            { "id": "bad.003", "text": "Bad tone.", "category": "computer", "tone": "sarcastic" },
            { "id": "bad.004", "text": "Bad category.", "category": "nonsense", "tone": "cheeky" },
            { "id": "bad.005", "text": "Bad weight.", "category": "computer", "tone": "cheeky", "weight": 0 },
            { "id": "bad.006", "text": "Bad window.", "category": "computer", "tone": "cheeky", "timeWindow": "brunch" }
          ]
        }
        """);

        var result = PhraseCatalog.Load(_tempDirectory, "xx-XX");

        Assert.Single(result.Phrases);
        Assert.Equal("ok.001", result.Phrases[0].Id);
        Assert.True(result.Diagnostics.Count >= 3);
    }
}

public sealed class PhraseSelectorTests
{
    private static readonly DateTime Noon = new(2026, 11, 21, 12, 0, 0);

    private static Phrase Make(
        string id,
        PhraseCategory category = PhraseCategory.Computer,
        ContentTone tone = ContentTone.Cheeky,
        int weight = 1,
        DayPart? window = null,
        int? minActive = null,
        bool familyFriendly = true) =>
        new(id, "Text " + id, category, tone, weight, window, minActive, familyFriendly);

    private static PhraseRequest Request(
        ContentTone tone = ContentTone.Unhinged,
        bool familyFriendly = false,
        DateTime? now = null,
        int sessionMinutes = 0,
        params PhraseCategory[] categories)
    {
        var enabled = categories.Length == 0
            ? new HashSet<PhraseCategory>(Enum.GetValues<PhraseCategory>())
            : new HashSet<PhraseCategory>(categories);

        return new PhraseRequest(tone, familyFriendly, enabled, now ?? Noon, sessionMinutes);
    }

    [Fact]
    public void DoesNotRepeatWithinTheHistoryWindow()
    {
        var phrases = new[] { Make("a"), Make("b"), Make("c") };
        var selector = new PhraseSelector(phrases, new Random(1234), historyLimit: 3);

        var selected = new HashSet<string>();
        for (var i = 0; i < 3; i++)
        {
            var phrase = selector.Select(Request());
            Assert.NotNull(phrase);
            selected.Add(phrase!.Id);
        }

        Assert.Equal(3, selected.Count);
    }

    [Fact]
    public void PoliteTone_ExcludesCheekyAndUnhinged()
    {
        var phrases = new[]
        {
            Make("polite", tone: ContentTone.Polite),
            Make("cheeky", tone: ContentTone.Cheeky),
            Make("unhinged", tone: ContentTone.Unhinged),
        };
        var selector = new PhraseSelector(phrases, new Random(7));

        var phrase = selector.Select(Request(ContentTone.Polite));

        Assert.Equal("polite", phrase!.Id);
    }

    [Fact]
    public void TimeWindow_ExcludesOutOfHoursLines()
    {
        var phrases = new[]
        {
            Make("morning", window: DayPart.Morning),
            Make("anytime"),
        };
        var selector = new PhraseSelector(phrases, new Random(7));

        var phrase = selector.Select(Request(now: new DateTime(2026, 11, 21, 20, 0, 0)));

        Assert.Equal("anytime", phrase!.Id);
    }

    [Fact]
    public void SessionAge_GatesReminderLines()
    {
        var phrases = new[] { Make("three-hours", minActive: 180) };

        var early = new PhraseSelector(phrases, new Random(1)).Select(Request(sessionMinutes: 60));
        var late = new PhraseSelector(phrases, new Random(1)).Select(Request(sessionMinutes: 180));

        Assert.Null(early);
        Assert.Equal("three-hours", late!.Id);
    }

    [Fact]
    public void FamilyFriendlyFilter_ExcludesFlaggedLines()
    {
        var phrases = new[]
        {
            Make("adult", familyFriendly: false),
            Make("clean"),
        };
        var selector = new PhraseSelector(phrases, new Random(3));

        var phrase = selector.Select(Request(familyFriendly: true));

        Assert.Equal("clean", phrase!.Id);
    }

    [Fact]
    public void DisabledCategories_AreExcluded()
    {
        var phrases = new[]
        {
            Make("computer", category: PhraseCategory.Computer),
            Make("break", category: PhraseCategory.Break),
        };
        var selector = new PhraseSelector(phrases, new Random(3));

        var phrase = selector.Select(Request(categories: PhraseCategory.Break));

        Assert.Equal("break", phrase!.Id);
    }

    [Fact]
    public void AllRecent_StillReturnsAPhraseInsteadOfNothing()
    {
        var phrases = new[] { Make("a"), Make("b") };
        var selector = new PhraseSelector(phrases, new Random(5), historyLimit: 2);

        selector.Select(Request());
        selector.Select(Request());
        var third = selector.Select(Request());

        Assert.NotNull(third);
    }

    [Theory]
    [InlineData(0, 0, false)]
    [InlineData(25, 24, true)]
    [InlineData(25, 25, false)]
    [InlineData(100, 99, true)]
    public void JokePolicy_RespectsTheSelectedPercentage(int percent, int roll, bool expected)
    {
        Assert.Equal(expected, JokePolicy.IncludeJoke(percent, roll));
    }
}
