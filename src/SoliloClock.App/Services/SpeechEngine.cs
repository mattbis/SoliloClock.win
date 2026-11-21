using System;
using System.Collections.Generic;
using System.Linq;
using System.Speech.Synthesis;

namespace SoliloClock.Services;

public sealed record SpeechOptions(int Rate, int Volume, string? VoiceName);

public interface ISpeechEngine : IDisposable
{
    bool IsAvailable { get; }

    string? UnavailableReason { get; }

    IReadOnlyList<string> Voices { get; }

    string? SelectedVoice { get; }

    event EventHandler<string>? SpeechFailed;

    bool TrySelectVoice(string? voiceName);

    void Speak(string text, SpeechOptions options);

    void Stop();
}

/// <summary>
/// Windows SAPI speech through System.Speech. Speech is queued asynchronously
/// and any outstanding utterance is cancelled before a new one starts, so a
/// long announcement can never stack up behind a stale one.
/// </summary>
public sealed class SapiSpeechEngine : ISpeechEngine
{
    private readonly object _gate = new();
    private readonly SpeechSynthesizer? _synthesizer;
    private bool _disposed;

    public SapiSpeechEngine()
    {
        try
        {
            _synthesizer = new SpeechSynthesizer
            {
                Rate = 0,
                Volume = 100,
            };
            _synthesizer.SetOutputToDefaultAudioDevice();
        }
        catch (Exception ex)
        {
            UnavailableReason = ex.Message;
        }
    }

    public bool IsAvailable => _synthesizer is not null;

    public string? UnavailableReason { get; }

    public string? SelectedVoice { get; private set; }

    public event EventHandler<string>? SpeechFailed;

    public IReadOnlyList<string> Voices
    {
        get
        {
            if (_synthesizer is null)
            {
                return Array.Empty<string>();
            }

            try
            {
                return _synthesizer.GetInstalledVoices()
                    .Where(voice => voice.Enabled)
                    .Select(voice => voice.VoiceInfo.Name)
                    .OrderBy(name => name, StringComparer.CurrentCulture)
                    .ToList();
            }
            catch (Exception ex)
            {
                RaiseFailed(ex.Message);
                return Array.Empty<string>();
            }
        }
    }

    public bool TrySelectVoice(string? voiceName)
    {
        if (_synthesizer is null)
        {
            return false;
        }

        if (string.IsNullOrWhiteSpace(voiceName))
        {
            SelectedVoice = null;
            return true;
        }

        try
        {
            if (!Voices.Contains(voiceName, StringComparer.CurrentCultureIgnoreCase))
            {
                return false;
            }

            _synthesizer.SelectVoice(voiceName);
            SelectedVoice = voiceName;
            return true;
        }
        catch (Exception ex)
        {
            RaiseFailed(ex.Message);
            return false;
        }
    }

    public void Speak(string text, SpeechOptions options)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return;
        }

        if (_synthesizer is null)
        {
            RaiseFailed("No Windows speech voice is available.");
            return;
        }

        lock (_gate)
        {
            if (_disposed)
            {
                return;
            }

            try
            {
                _synthesizer.Rate = Math.Clamp(options.Rate, -10, 10);
                _synthesizer.Volume = Math.Clamp(options.Volume, 0, 100);

                if (!string.IsNullOrWhiteSpace(options.VoiceName) &&
                    !string.Equals(options.VoiceName, SelectedVoice, StringComparison.CurrentCultureIgnoreCase))
                {
                    TrySelectVoice(options.VoiceName);
                }

                // Never let a stale or long utterance stack up.
                _synthesizer.SpeakAsyncCancelAll();
                _synthesizer.SpeakAsync(text);
            }
            catch (Exception ex)
            {
                RaiseFailed(ex.Message);
            }
        }
    }

    public void Stop()
    {
        if (_synthesizer is null)
        {
            return;
        }

        lock (_gate)
        {
            try
            {
                _synthesizer.SpeakAsyncCancelAll();
            }
            catch (Exception ex)
            {
                RaiseFailed(ex.Message);
            }
        }
    }

    public void Dispose()
    {
        lock (_gate)
        {
            if (_disposed)
            {
                return;
            }

            _disposed = true;
            try
            {
                _synthesizer?.SpeakAsyncCancelAll();
                _synthesizer?.Dispose();
            }
            catch
            {
                // Shutdown must never throw.
            }
        }
    }

    private void RaiseFailed(string message) => SpeechFailed?.Invoke(this, message);
}
