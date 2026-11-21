using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.Json;
using SoliloClock.Models;

namespace SoliloClock.Services;

public sealed record SettingsLoadResult(AppSettings Settings, bool Recovered, string? Message);

/// <summary>
/// Loads and saves settings atomically. A malformed file is preserved with an
/// ".invalid-..." suffix and defaults are used instead of failing to start.
/// </summary>
public sealed class SettingsService
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
    };

    private readonly object _gate = new();
    private readonly string _directory;

    public SettingsService(string? directory = null)
    {
        _directory = directory ?? Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "SoliloClock");
        FilePath = Path.Combine(_directory, "settings.json");
    }

    public string FilePath { get; }

    public SettingsLoadResult Load()
    {
        lock (_gate)
        {
            if (!File.Exists(FilePath))
            {
                return new SettingsLoadResult(new AppSettings(), false, null);
            }

            try
            {
                var json = File.ReadAllText(FilePath, Encoding.UTF8);
                var settings = JsonSerializer.Deserialize<AppSettings>(json, JsonOptions);
                return new SettingsLoadResult(Normalize(settings), false, null);
            }
            catch (Exception ex) when (ex is JsonException or IOException or UnauthorizedAccessException or NotSupportedException)
            {
                var backup = TryBackupBrokenFile();
                var message = backup is null
                    ? "Settings could not be read; defaults restored. The unreadable file was left in place."
                    : $"Settings could not be read; defaults restored. The old file was saved as {Path.GetFileName(backup)}.";
                return new SettingsLoadResult(new AppSettings(), true, message);
            }
        }
    }

    public void Save(AppSettings settings)
    {
        ArgumentNullException.ThrowIfNull(settings);

        lock (_gate)
        {
            Directory.CreateDirectory(_directory);
            var json = JsonSerializer.Serialize(settings, JsonOptions);
            var tempPath = FilePath + ".tmp";

            File.WriteAllText(tempPath, json, new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));

            if (File.Exists(FilePath))
            {
                try
                {
                    File.Replace(tempPath, FilePath, destinationBackupFileName: null);
                }
                catch (IOException)
                {
                    File.Copy(tempPath, FilePath, overwrite: true);
                    File.Delete(tempPath);
                }
            }
            else
            {
                File.Move(tempPath, FilePath);
            }
        }
    }

    private string? TryBackupBrokenFile()
    {
        try
        {
            var backup = Path.Combine(
                _directory,
                $"settings.invalid-{DateTime.Now:yyyyMMdd-HHmmss}.json");
            File.Copy(FilePath, backup, overwrite: false);
            return backup;
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            return null;
        }
    }

    public static AppSettings Normalize(AppSettings? settings)
    {
        settings ??= new AppSettings();

        if (!AnnouncementIntervals.IsSupported(settings.IntervalMinutes))
        {
            settings.IntervalMinutes = 15;
        }

        settings.Theme = ThemeCatalog.Get(settings.Theme).Id;

        if (double.IsNaN(settings.WindowOpacity) || double.IsInfinity(settings.WindowOpacity))
        {
            settings.WindowOpacity = 0.92;
        }

        settings.WindowOpacity = Math.Clamp(settings.WindowOpacity, 0.4, 1.0);

        if (settings.JokePercent is not (0 or 10 or 25 or 50 or 100))
        {
            settings.JokePercent = 25;
        }

        if (!ContentTones.TryParse(settings.Tone, out var tone))
        {
            tone = ContentTone.Cheeky;
        }

        settings.Tone = ContentTones.ToName(tone);

        settings.SpeechRate = Math.Clamp(settings.SpeechRate, -10, 10);
        settings.SpeechVolume = Math.Clamp(settings.SpeechVolume, 0, 100);

        if (string.IsNullOrWhiteSpace(settings.UiLanguage))
        {
            settings.UiLanguage = "en-GB";
        }

        return settings;
    }
}
