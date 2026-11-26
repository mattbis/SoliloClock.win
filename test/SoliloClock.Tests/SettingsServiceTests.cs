using System;
using System.IO;
using System.Linq;
using SoliloClock.Models;
using SoliloClock.Services;
using Xunit;

namespace SoliloClock.Tests;

public sealed class SettingsServiceTests : IDisposable
{
    private readonly string _directory;

    public SettingsServiceTests()
    {
        _directory = Path.Combine(
            Path.GetTempPath(),
            "soliloclock-tests-" + Guid.NewGuid().ToString("N"));
    }

    public void Dispose()
    {
        try
        {
            if (Directory.Exists(_directory))
            {
                Directory.Delete(_directory, recursive: true);
            }
        }
        catch (IOException)
        {
        }
    }

    [Fact]
    public void MissingFile_ReturnsDefaults()
    {
        var result = new SettingsService(_directory).Load();

        Assert.False(result.Recovered);
        Assert.Equal(15, result.Settings.IntervalMinutes);
        Assert.Equal(25, result.Settings.JokePercent);
        Assert.Equal("pink", result.Settings.Theme);
        Assert.False(result.Settings.Muted);
        Assert.Null(result.Settings.UiLanguage);
    }

    [Fact]
    public void SaveThenLoad_RoundTripsValues()
    {
        var service = new SettingsService(_directory);
        var anchor = new DateTimeOffset(2026, 11, 21, 10, 7, 0, TimeSpan.FromHours(1));
        var settings = new AppSettings
        {
            IntervalMinutes = 45,
            Anchor45 = anchor,
            Muted = true,
            Theme = "neon",
            AlwaysOnTop = true,
            Use24Hour = true,
            JokePercent = 50,
            Tone = "unhinged",
            SpeechVoice = "Microsoft David Desktop",
            WindowOpacity = 0.71,
        };

        service.Save(settings);
        var result = service.Load();

        Assert.False(result.Recovered);
        Assert.Equal(45, result.Settings.IntervalMinutes);
        Assert.Equal(anchor, result.Settings.Anchor45);
        Assert.True(result.Settings.Muted);
        Assert.Equal("neon", result.Settings.Theme);
        Assert.True(result.Settings.AlwaysOnTop);
        Assert.True(result.Settings.Use24Hour);
        Assert.Equal(50, result.Settings.JokePercent);
        Assert.Equal("unhinged", result.Settings.Tone);
        Assert.Equal(0.71, result.Settings.WindowOpacity, 3);
    }

    [Fact]
    public void MalformedFile_RecoversDefaultsAndKeepsABackup()
    {
        Directory.CreateDirectory(_directory);
        File.WriteAllText(Path.Combine(_directory, "settings.json"), "{ this is not json");

        var result = new SettingsService(_directory).Load();

        Assert.True(result.Recovered);
        Assert.Equal(15, result.Settings.IntervalMinutes);
        Assert.NotEmpty(Directory.GetFiles(_directory, "settings.invalid-*.json"));
        Assert.False(string.IsNullOrWhiteSpace(result.BackupFileName));
        Assert.True(File.Exists(Path.Combine(_directory, result.BackupFileName!)));
    }

    [Fact]
    public void Normalize_ClampsAndRepairsInvalidValues()
    {
        var settings = new AppSettings
        {
            IntervalMinutes = 17,
            JokePercent = 33,
            Tone = "sarcastic",
            Theme = "nonexistent",
            WindowOpacity = 9,
            SpeechRate = 99,
            SpeechVolume = -5,
        };

        var normalized = SettingsService.Normalize(settings);

        Assert.Equal(15, normalized.IntervalMinutes);
        Assert.Equal(25, normalized.JokePercent);
        Assert.Equal("cheeky", normalized.Tone);
        Assert.Equal("pink", normalized.Theme);
        Assert.Equal(1.0, normalized.WindowOpacity, 3);
        Assert.Equal(10, normalized.SpeechRate);
        Assert.Equal(0, normalized.SpeechVolume);
    }

    [Fact]
    public void Save_LeavesNoTemporaryFileBehind()
    {
        var service = new SettingsService(_directory);
        service.Save(new AppSettings());
        service.Save(new AppSettings { Muted = true });

        Assert.Empty(Directory.GetFiles(_directory, "*.tmp"));
    }

    [Fact]
    public void Normalize_TurnsUnknownLanguageTagsIntoAutomatic()
    {
        var broken = SettingsService.Normalize(new AppSettings
        {
            UiLanguage = "not a locale",
            SpeechLanguage = "   ",
        });

        Assert.Null(broken.UiLanguage);
        Assert.Null(broken.SpeechLanguage);

        var automatic = SettingsService.Normalize(new AppSettings
        {
            UiLanguage = "AUTO",
            SpeechLanguage = "system",
        });

        Assert.Null(automatic.UiLanguage);
        Assert.Null(automatic.SpeechLanguage);

        var pinned = SettingsService.Normalize(new AppSettings
        {
            UiLanguage = "fr-fr",
            SpeechLanguage = "de-DE",
        });

        Assert.Equal("fr-FR", pinned.UiLanguage);
        Assert.Equal("de-DE", pinned.SpeechLanguage);
    }
}
