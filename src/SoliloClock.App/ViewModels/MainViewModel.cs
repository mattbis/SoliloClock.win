using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Input;
using System.Windows.Threading;
using Microsoft.Win32;
using SoliloClock.Models;
using SoliloClock.Services;

namespace SoliloClock.ViewModels;

public sealed record Notification(string Title, string Message);

public sealed class MainViewModel : INotifyPropertyChanged, IDisposable
{
    private static readonly IReadOnlySet<PhraseCategory> AllCategories =
        new HashSet<PhraseCategory>(Enum.GetValues<PhraseCategory>());

    private readonly SettingsService _settingsService;
    private readonly AppSettings _settings;
    private readonly ISpeechEngine _speech;
    private readonly PhraseSelector _selector;
    private readonly IClock _clock;
    private readonly ScheduleCalculator _calculator;
    private readonly AnnouncementSchedule _schedule;
    private readonly EnglishTimeFormatter _formatter = new();
    private readonly PhraseLoadResult _catalog;
    private readonly DateTimeOffset _startedAt;
    private readonly Random _random = new();

    private readonly DispatcherTimer _announcementTimer;
    private readonly DispatcherTimer _uiTimer;
    private readonly DispatcherTimer _saveTimer;

    private bool _started;
    private bool _disposed;
    private string _caption = "Ready when you are.";
    private string _status = string.Empty;
    private string _digitalTime = string.Empty;
    private string _nextAnnouncementText = "No announcement scheduled";
    private string _themeId = "pink";
    private string _themeDisplayName = "Pink";
    private bool _sparkles = true;
    private bool _glow;
    private bool _isTransparentTheme;

    public MainViewModel(
        SettingsService settingsService,
        AppSettings settings,
        ISpeechEngine speech,
        PhraseSelector selector,
        IClock clock,
        PhraseLoadResult catalog)
    {
        _settingsService = settingsService ?? throw new ArgumentNullException(nameof(settingsService));
        _settings = settings ?? throw new ArgumentNullException(nameof(settings));
        _speech = speech ?? throw new ArgumentNullException(nameof(speech));
        _selector = selector ?? throw new ArgumentNullException(nameof(selector));
        _clock = clock ?? throw new ArgumentNullException(nameof(clock));
        _catalog = catalog ?? throw new ArgumentNullException(nameof(catalog));
        _calculator = new ScheduleCalculator();
        _schedule = new AnnouncementSchedule(clock, _calculator);
        _startedAt = clock.Now;

        SelectIntervalCommand = new RelayCommand(ExecuteSelectInterval);
        SpeakNowCommand = new RelayCommand(_ => SpeakNow());
        CycleThemeCommand = new RelayCommand(_ => CycleTheme());

        _speech.SpeechFailed += OnSpeechFailed;

        _announcementTimer = new DispatcherTimer(DispatcherPriority.Background);
        _announcementTimer.Tick += OnAnnouncementTimerTick;

        _uiTimer = new DispatcherTimer(DispatcherPriority.Background)
        {
            Interval = TimeSpan.FromSeconds(1),
        };
        _uiTimer.Tick += (_, _) => UpdateLiveTexts();

        _saveTimer = new DispatcherTimer(DispatcherPriority.Background)
        {
            Interval = TimeSpan.FromMilliseconds(700),
        };
        _saveTimer.Tick += (_, _) => SaveNow();

        if (_settings.SpeechVoice is not null && !_speech.TrySelectVoice(_settings.SpeechVoice))
        {
            _status = $"The saved voice '{_settings.SpeechVoice}' is not installed; using the default Windows voice.";
            _settings.SpeechVoice = null;
        }

        ApplyTheme(ThemeCatalog.Get(_settings.Theme), save: false);
        _digitalTime = FormatDigital(DateTime.Now);
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    public event EventHandler? StateChanged;

    public event EventHandler<Notification>? NotifyRequested;

    public ICommand SelectIntervalCommand { get; }

    public ICommand SpeakNowCommand { get; }

    public ICommand CycleThemeCommand { get; }

    public AnnouncementInterval Interval => AnnouncementIntervals.FromMinutes(_settings.IntervalMinutes);

    public bool IsInterval15 => _settings.IntervalMinutes == 15;

    public bool IsInterval30 => _settings.IntervalMinutes == 30;

    public bool IsInterval45 => _settings.IntervalMinutes == 45;

    public bool IsInterval60 => _settings.IntervalMinutes == 60;

    public string Caption
    {
        get => _caption;
        private set => SetProperty(ref _caption, value);
    }

    public string Status
    {
        get => _status;
        private set => SetProperty(ref _status, value);
    }

    public string DigitalTime
    {
        get => _digitalTime;
        private set => SetProperty(ref _digitalTime, value);
    }

    public string NextAnnouncementText
    {
        get => _nextAnnouncementText;
        private set => SetProperty(ref _nextAnnouncementText, value);
    }

    public string ThemeId
    {
        get => _themeId;
        private set => SetProperty(ref _themeId, value);
    }

    public string ThemeDisplayName
    {
        get => _themeDisplayName;
        private set => SetProperty(ref _themeDisplayName, value);
    }

    public bool Sparkles
    {
        get => _sparkles;
        private set => SetProperty(ref _sparkles, value);
    }

    public bool Glow
    {
        get => _glow;
        private set => SetProperty(ref _glow, value);
    }

    public bool IsTransparentTheme
    {
        get => _isTransparentTheme;
        private set
        {
            if (SetProperty(ref _isTransparentTheme, value))
            {
                OnPropertyChanged(nameof(EffectiveWindowOpacity));
            }
        }
    }

    public bool IsMuted
    {
        get => _settings.Muted;
        set
        {
            if (_settings.Muted == value)
            {
                return;
            }

            _settings.Muted = value;
            if (value)
            {
                _speech.Stop();
            }

            OnPropertyChanged();
            OnPropertyChanged(nameof(MuteGlyph));
            OnPropertyChanged(nameof(MuteTooltip));
            ScheduleSave();
            RaiseStateChanged();
        }
    }

    public string MuteGlyph => IsMuted ? "\uE74F" : "\uE767";

    public string MuteTooltip => IsMuted ? "Unmute scheduled announcements" : "Mute scheduled announcements";

    public bool AlwaysOnTop
    {
        get => _settings.AlwaysOnTop;
        set
        {
            if (_settings.AlwaysOnTop == value)
            {
                return;
            }

            _settings.AlwaysOnTop = value;
            OnPropertyChanged();
            ScheduleSave();
            RaiseStateChanged();
        }
    }

    public double WindowOpacity
    {
        get => _settings.WindowOpacity;
        set
        {
            var clamped = Math.Clamp(value, 0.45, 1.0);
            if (Math.Abs(_settings.WindowOpacity - clamped) < 0.001)
            {
                return;
            }

            _settings.WindowOpacity = clamped;
            OnPropertyChanged();
            OnPropertyChanged(nameof(EffectiveWindowOpacity));
            ScheduleSave();
        }
    }

    public double EffectiveWindowOpacity => IsTransparentTheme ? WindowOpacity : 1.0;

    public bool ReducedMotionSetting
    {
        get => _settings.ReducedMotion;
        set
        {
            if (_settings.ReducedMotion == value)
            {
                return;
            }

            _settings.ReducedMotion = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(ReducedMotion));
            ScheduleSave();
        }
    }

    /// <summary>Reduced motion is on when requested or when Windows animations are off.</summary>
    public bool ReducedMotion => _settings.ReducedMotion || !SystemParameters.ClientAreaAnimation;

    public bool ShouldExplainClose => !_settings.CloseToTrayExplained;

    public string ShortStatus =>
        $"Talking Clock — {ThemeDisplayName} · {NextAnnouncementText}";

    public string? LastSpokenDisplay =>
        _settings.LastSpokenAt is { } last ? last.ToLocalTime().ToString("t") : null;

    public void Start()
    {
        if (_started || _disposed)
        {
            return;
        }

        _started = true;

        if (Interval == AnnouncementInterval.Minutes45 && _settings.Anchor45 is null)
        {
            _settings.Anchor45 = _clock.Now;
            ScheduleSave();
        }

        _schedule.Configure(Interval, _settings.Anchor45);
        ArmAnnouncementTimer();
        UpdateLiveTexts();

        var startupNotes = new List<string>();
        if (_catalog.Diagnostics.Count > 0)
        {
            startupNotes.Add(_catalog.Diagnostics[0]);
        }

        if (!_speech.IsAvailable)
        {
            startupNotes.Add("No Windows speech voice is available; captions still work.");
        }
        else if (_speech.Voices.Count == 0)
        {
            startupNotes.Add("Windows reports no installed voices; announcements will be silent.");
        }

        if (_catalog.Phrases.Count == 0)
        {
            startupNotes.Add("No phrases loaded; only time announcements will be spoken.");
        }

        if (startupNotes.Count > 0)
        {
            Status = string.Join(" ", startupNotes);
        }

        SystemEvents.TimeChanged += OnSystemTimeChanged;
        SystemEvents.PowerModeChanged += OnPowerModeChanged;
        SystemEvents.SessionSwitch += OnSessionSwitch;

        RaiseStateChanged();

        if (!_speech.IsAvailable)
        {
            NotifyRequested?.Invoke(
                this,
                new Notification(
                    "Speech unavailable",
                    "No Windows speech voice was found. The clock still works and shows captions. Install a Windows voice to hear announcements."));
        }
    }

    public void SetWindowVisible(bool visible)
    {
        if (visible)
        {
            UpdateLiveTexts();
            if (!_uiTimer.IsEnabled)
            {
                _uiTimer.Start();
            }
        }
        else
        {
            _uiTimer.Stop();
        }
    }

    public void SelectInterval(AnnouncementInterval interval)
    {
        if (interval == Interval)
        {
            return;
        }

        _settings.IntervalMinutes = (int)interval;
        _settings.Anchor45 = interval == AnnouncementInterval.Minutes45 ? _clock.Now : null;

        _schedule.Configure(interval, _settings.Anchor45);
        NotifyIntervalProperties();
        ArmAnnouncementTimer();
        UpdateLiveTexts();
        ScheduleSave();

        Status = interval == AnnouncementInterval.Minutes45
            ? "45-minute cadence anchored to now."
            : $"Announcing every {(int)interval} minutes.";
    }

    /// <summary>Speaks immediately, even when scheduled announcements are muted.</summary>
    public void SpeakNow()
    {
        var local = DateTime.Now;
        var timeText = _formatter.Format(local, new TimeFormatOptions(_settings.UseNaturalTime, _settings.Use24Hour));
        var joke = TrySelectJoke(local);
        var utterance = joke is null ? timeText : $"{timeText} {joke}";

        Caption = utterance;
        Status = _speech.IsAvailable ? "Speaking now." : "No speech voice available; showing the caption instead.";
        _speech.Speak(utterance, CurrentSpeechOptions);
        _settings.LastSpokenAt = _clock.Now;
        ScheduleSave();
    }

    public void SetTheme(string themeId)
    {
        var theme = ThemeCatalog.Get(themeId);
        if (!string.Equals(theme.Id, ThemeId, StringComparison.OrdinalIgnoreCase))
        {
            ApplyTheme(theme, save: true);
        }
    }

    public void CycleTheme()
    {
        var themes = ThemeCatalog.All;
        var index = 0;
        for (var i = 0; i < themes.Count; i++)
        {
            if (string.Equals(themes[i].Id, ThemeId, StringComparison.OrdinalIgnoreCase))
            {
                index = i;
                break;
            }
        }

        SetTheme(themes[(index + 1) % themes.Count].Id);
    }

    public void MarkCloseToTrayExplained()
    {
        if (_settings.CloseToTrayExplained)
        {
            return;
        }

        _settings.CloseToTrayExplained = true;
        SaveNow();
    }

    public void ReportStatus(string message)
    {
        if (string.IsNullOrWhiteSpace(message))
        {
            return;
        }

        Status = string.IsNullOrWhiteSpace(Status) ? message : $"{message} {Status}";
    }

    public void SaveNow()
    {
        _saveTimer.Stop();
        try
        {
            _settingsService.Save(_settings);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            Status = $"Settings could not be saved: {ex.Message}";
        }
    }

    public void Shutdown()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;

        _announcementTimer.Stop();
        _uiTimer.Stop();
        _saveTimer.Stop();

        SystemEvents.TimeChanged -= OnSystemTimeChanged;
        SystemEvents.PowerModeChanged -= OnPowerModeChanged;
        SystemEvents.SessionSwitch -= OnSessionSwitch;

        _speech.Stop();
        _speech.Dispose();
        _speech.SpeechFailed -= OnSpeechFailed;

        SaveNow();
    }

    public void Dispose() => Shutdown();

    private SpeechOptions CurrentSpeechOptions =>
        new(_settings.SpeechRate, _settings.SpeechVolume, _settings.SpeechVoice);

    private Dispatcher Dispatcher => Application.Current?.Dispatcher ?? Dispatcher.CurrentDispatcher;

    private void ExecuteSelectInterval(object? parameter)
    {
        if (parameter is string text && int.TryParse(text, out var minutes) && AnnouncementIntervals.IsSupported(minutes))
        {
            SelectInterval(AnnouncementIntervals.FromMinutes(minutes));
        }
        else if (parameter is AnnouncementInterval interval)
        {
            SelectInterval(interval);
        }
    }

    private void ApplyTheme(ThemeDefinition theme, bool save)
    {
        ThemeManager.Apply(theme);
        _settings.Theme = theme.Id;

        ThemeId = theme.Id;
        ThemeDisplayName = theme.DisplayName;
        Sparkles = theme.Sparkles;
        Glow = theme.Glow;
        IsTransparentTheme = theme.IsTransparent;

        if (save)
        {
            ScheduleSave();
        }

        RaiseStateChanged();
    }

    private void NotifyIntervalProperties()
    {
        OnPropertyChanged(nameof(Interval));
        OnPropertyChanged(nameof(IsInterval15));
        OnPropertyChanged(nameof(IsInterval30));
        OnPropertyChanged(nameof(IsInterval45));
        OnPropertyChanged(nameof(IsInterval60));
    }

    private void OnAnnouncementTimerTick(object? sender, EventArgs e)
    {
        _announcementTimer.Stop();

        if (_schedule.TryConsumeDue(out var dueAt))
        {
            Announce(dueAt);
        }

        ArmAnnouncementTimer();
    }

    private void ArmAnnouncementTimer()
    {
        _announcementTimer.Stop();

        var next = _schedule.Next;
        if (next is null)
        {
            NextAnnouncementText = "No announcement scheduled";
            return;
        }

        var delay = next.Value - _clock.Now;
        if (delay < TimeSpan.FromMilliseconds(300))
        {
            delay = TimeSpan.FromMilliseconds(300);
        }

        // Re-check at least hourly so external clock changes are noticed even
        // if a system event is missed. This is a single-shot timer, not polling.
        if (delay > TimeSpan.FromMinutes(61))
        {
            delay = TimeSpan.FromMinutes(61);
        }

        _announcementTimer.Interval = delay;
        _announcementTimer.Start();
        NextAnnouncementText = BuildNextAnnouncementText(next.Value);
        RaiseStateChanged();
    }

    private void Announce(DateTimeOffset when)
    {
        if (_settings.Muted)
        {
            return;
        }

        var local = TimeZoneInfo.ConvertTime(when, TimeZoneInfo.Local).DateTime;
        var timeText = _formatter.Format(local, new TimeFormatOptions(_settings.UseNaturalTime, _settings.Use24Hour));
        var joke = TrySelectJoke(local);
        var utterance = joke is null ? timeText : $"{timeText} {joke}";

        Caption = utterance;
        Status = $"Spoken at {local:HH:mm}.";
        _settings.LastSpokenAt = _clock.Now;
        ScheduleSave();

        _speech.Speak(utterance, CurrentSpeechOptions);
    }

    private string? TrySelectJoke(DateTime localNow)
    {
        if (!JokePolicy.IncludeJoke(_settings.JokePercent, _random.Next(100)))
        {
            return null;
        }

        if (!ContentTones.TryParse(_settings.Tone, out var tone))
        {
            tone = ContentTone.Cheeky;
        }

        var sessionMinutes = (int)Math.Max(0, (_clock.Now - _startedAt).TotalMinutes);
        var request = new PhraseRequest(
            tone,
            _settings.FamilyFriendly,
            AllCategories,
            localNow,
            sessionMinutes);

        return _selector.Select(request)?.Text;
    }

    private void UpdateLiveTexts()
    {
        DigitalTime = FormatDigital(DateTime.Now);

        var next = _schedule.Next;
        NextAnnouncementText = next is { } value
            ? BuildNextAnnouncementText(value)
            : "No announcement scheduled";
    }

    private string BuildNextAnnouncementText(DateTimeOffset next)
    {
        var local = next.ToLocalTime();
        var time = _settings.Use24Hour ? local.ToString("HH:mm") : local.ToString("h:mm tt");
        var minutes = (int)Math.Ceiling((next - _clock.Now).TotalMinutes);
        var relative = minutes <= 0 ? "now" : $"in {minutes} min";
        return $"Next {time} · {relative}";
    }

    private string FormatDigital(DateTime now) =>
        _settings.Use24Hour ? now.ToString("HH:mm:ss") : now.ToString("h:mm:ss tt");

    private void ScheduleSave()
    {
        _saveTimer.Stop();
        _saveTimer.Start();
    }

    private void OnSystemTimeChanged(object? sender, EventArgs e) =>
        Dispatch(RescheduleAfterSystemChange);

    private void OnPowerModeChanged(object? sender, PowerModeChangedEventArgs e)
    {
        if (e.Mode == PowerModes.Resume)
        {
            Dispatch(RescheduleAfterSystemChange);
        }
    }

    private void OnSessionSwitch(object? sender, SessionSwitchEventArgs e)
    {
        if (e.Reason is SessionSwitchReason.SessionUnlock or SessionSwitchReason.SessionLogon)
        {
            Dispatch(RescheduleAfterSystemChange);
        }
    }

    private void RescheduleAfterSystemChange()
    {
        if (_disposed)
        {
            return;
        }

        _schedule.Reschedule();
        ArmAnnouncementTimer();
        UpdateLiveTexts();
    }

    private void Dispatch(Action action)
    {
        var dispatcher = Dispatcher;
        if (dispatcher.CheckAccess())
        {
            action();
        }
        else
        {
            dispatcher.BeginInvoke(action);
        }
    }

    private void OnSpeechFailed(object? sender, string message) =>
        Dispatch(() => Status = $"Speech error: {message}");

    private void RaiseStateChanged() => StateChanged?.Invoke(this, EventArgs.Empty);

    private bool SetProperty<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value))
        {
            return false;
        }

        field = value;
        OnPropertyChanged(propertyName);
        return true;
    }

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
}
