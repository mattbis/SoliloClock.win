using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Input;
using System.Windows.Markup;
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
    private PhraseSelector _selector;
    private readonly IClock _clock;
    private readonly ScheduleCalculator _calculator;
    private readonly AnnouncementSchedule _schedule;
    private UiText _text;
    private TimeFormatterSelection _formatter;
    private PhraseLoadResult _catalog;
    private readonly DateTimeOffset _startedAt;
    private readonly Random _random = new();

    private readonly DispatcherTimer _announcementTimer;
    private readonly DispatcherTimer _uiTimer;
    private readonly DispatcherTimer _saveTimer;

    private bool _started;
    private bool _disposed;
    private string _caption = string.Empty;
    private string _status = string.Empty;
    private string _digitalTime = string.Empty;
    private string _nextAnnouncementText = string.Empty;
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
        PhraseLoadResult catalog,
        UiText text,
        TimeFormatterSelection formatter)
    {
        _settingsService = settingsService ?? throw new ArgumentNullException(nameof(settingsService));
        _settings = settings ?? throw new ArgumentNullException(nameof(settings));
        _speech = speech ?? throw new ArgumentNullException(nameof(speech));
        _selector = selector ?? throw new ArgumentNullException(nameof(selector));
        _clock = clock ?? throw new ArgumentNullException(nameof(clock));
        _catalog = catalog ?? throw new ArgumentNullException(nameof(catalog));
        _text = text ?? throw new ArgumentNullException(nameof(text));
        _formatter = formatter ?? throw new ArgumentNullException(nameof(formatter));
        Strings = new UiStrings(_text);
        _caption = _text[LocalizationKeys.CaptionReady];
        _nextAnnouncementText = _text[LocalizationKeys.StatusNoAnnouncement];
        _calculator = new ScheduleCalculator();
        _schedule = new AnnouncementSchedule(clock, _calculator);
        _startedAt = clock.Now;

        SelectIntervalCommand = new RelayCommand(ExecuteSelectInterval);
        SpeakNowCommand = new RelayCommand(_ => SpeakNow());
        CycleThemeCommand = new RelayCommand(_ => CycleTheme());
        TestVoiceCommand = new RelayCommand(_ => TestVoice());

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
            _status = _text.Format(LocalizationKeys.StatusSavedVoiceMissing, _settings.SpeechVoice);
            _settings.SpeechVoice = null;
        }

        ApplyTheme(ThemeCatalog.Get(_settings.Theme), save: false);
        _digitalTime = FormatDigital(DateTime.Now);
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    public event EventHandler? StateChanged;

    public event EventHandler<Notification>? NotifyRequested;

    /// <summary>Bindable localized window chrome, replaced when the language changes.</summary>
    public UiStrings Strings { get; private set; }

    public UiText Text => _text;

    public FlowDirection FlowDirection => _text.FlowDirection;

    public XmlLanguage WpfLanguage => _text.WpfLanguage;

    public ICommand SelectIntervalCommand { get; }

    public ICommand SpeakNowCommand { get; }

    public ICommand CycleThemeCommand { get; }

    public ICommand TestVoiceCommand { get; }

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

    public string MuteTooltip => _text[IsMuted ? LocalizationKeys.Unmute : LocalizationKeys.Mute];

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

    public IReadOnlyList<VoiceOption> Voices => _speech.Voices;

    public string? SelectedVoice => _settings.SpeechVoice;

    /// <summary>Language used for spoken grammar and the phrase catalog.</summary>
    public string AnnouncementLocale =>
        LocaleResolver.ResolveSpeech(_settings.SpeechLanguage, _text.Locale).Locale;

    /// <summary>The shipped interface languages, in rollout order.</summary>
    public IReadOnlyList<LocaleDefinition> ShippedLocales { get; } =
        LocaleRegistry.All.Where(locale => locale.IsShipped).ToList();

    /// <summary>Native name of the Windows display language, shown by the picker.</summary>
    public string SystemLanguageName => LocaleRegistry.GetDisplayName(LocaleResolver.SystemLocale);

    /// <summary>True while the interface follows the Windows language.</summary>
    public bool FollowsSystemLanguage => LocaleResolver.IsAutomatic(_settings.UiLanguage);

    /// <summary>True while announcements follow the interface language.</summary>
    public bool SpeechFollowsUi => LocaleResolver.IsAutomatic(_settings.SpeechLanguage);

    public string? UiLanguageSetting => SettingsService.NormalizeLanguageSetting(_settings.UiLanguage);

    public string? SpeechLanguageSetting => SettingsService.NormalizeLanguageSetting(_settings.SpeechLanguage);

    public string AnnouncementLanguageName => LocaleRegistry.GetDisplayName(AnnouncementLocale);

    /// <summary>True when an installed Windows voice can actually speak the announcement language.</summary>
    public bool HasVoiceForLanguage => VoiceCatalog.SpeaksLanguage(Voices, AnnouncementLocale);

    /// <summary>
    /// The voice used while none is pinned: an installed voice that speaks the
    /// announcement language, or null when only other languages are installed.
    /// Read live from Windows, so a voice installed later is used without a restart.
    /// </summary>
    public string? AutomaticVoice => VoiceCatalog.PreferredFor(Voices, AnnouncementLocale)?.Name;

    /// <summary>The pinned voice, otherwise the language-appropriate automatic one.</summary>
    public string? EffectiveVoice => _settings.SpeechVoice ?? AutomaticVoice;

    public string ShortStatus =>
        _text.Format(LocalizationKeys.StatusShort, ThemeDisplayName, NextAnnouncementText);

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

        var startupNotes = BuildLanguageNotes();
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
                    _text[LocalizationKeys.NotifySpeechUnavailableTitle],
                    _text[LocalizationKeys.NotifySpeechUnavailableMessage]));
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

        Status = IntervalStatusText();
    }

    /// <summary>Speaks immediately, even when scheduled announcements are muted.</summary>
    public void SpeakNow()
    {
        var local = DateTime.Now;
        var timeText = _formatter.Formatter.Format(local, new TimeFormatOptions(_settings.UseNaturalTime, _settings.Use24Hour));
        var joke = TrySelectJoke(local);
        var utterance = joke is null ? timeText : $"{timeText} {joke}";

        Caption = utterance;
        Status = _text[_speech.IsAvailable
            ? LocalizationKeys.StatusSpeakingNow
            : LocalizationKeys.StatusNoVoiceCaption];
        _speech.Speak(utterance, CurrentSpeechOptions);
        _settings.LastSpokenAt = _clock.Now;
        ScheduleSave();
    }

    /// <summary>Selects an installed voice, or null for the Windows default.</summary>
    public void SetVoice(string? voiceName)
    {
        if (string.Equals(_settings.SpeechVoice, voiceName, StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        if (voiceName is not null && !_speech.TrySelectVoice(voiceName))
        {
            Status = _text.Format(LocalizationKeys.StatusSavedVoiceMissing, voiceName);
            return;
        }

        _settings.SpeechVoice = voiceName;

        // Selecting the automatic option takes effect immediately rather than
        // waiting for the next announcement to switch voices.
        _speech.TrySelectVoice(EffectiveVoice);

        OnPropertyChanged(nameof(SelectedVoice));
        OnPropertyChanged(nameof(AutomaticVoice));
        OnPropertyChanged(nameof(EffectiveVoice));
        ScheduleSave();
        RaiseStateChanged();
    }

    /// <summary>Speaks a short sample so a voice can be judged before it is trusted.</summary>
    public void TestVoice()
    {
        if (!_speech.IsAvailable)
        {
            Status = _text[LocalizationKeys.StatusNoVoice];
            return;
        }

        _speech.Speak(_text[LocalizationKeys.VoiceTestSample], CurrentSpeechOptions);
    }

    /// <summary>
    /// Switches the interface language immediately. Null follows the Windows
    /// display language; an explicit tag overrides it.
    /// </summary>
    public void SetUiLanguage(string? languageTag)
    {
        var normalized = SettingsService.NormalizeLanguageSetting(languageTag);
        if (string.Equals(_settings.UiLanguage, normalized, StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        _settings.UiLanguage = normalized;
        ApplyUiLanguage();

        if (SpeechFollowsUi)
        {
            ApplySpeechLanguage();
        }

        Status = CurrentLanguageStatus();
        ScheduleSave();
        RaiseStateChanged();
    }

    /// <summary>
    /// Chooses the announcement language independently of the interface. Null
    /// follows the interface, which is the default.
    /// </summary>
    public void SetSpeechLanguage(string? languageTag)
    {
        var normalized = SettingsService.NormalizeLanguageSetting(languageTag);
        var stored = string.Equals(normalized, _text.Locale, StringComparison.OrdinalIgnoreCase)
            ? null
            : normalized;

        if (string.Equals(_settings.SpeechLanguage, stored, StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        _settings.SpeechLanguage = stored;
        ApplySpeechLanguage();
        Status = CurrentLanguageStatus();
        ScheduleSave();
        RaiseStateChanged();
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

    /// <summary>
    /// Reports that the settings file could not be read. The wording belongs to
    /// the view model rather than the loader: the loader runs before any catalog
    /// is available, and the language setting is usually the data that was lost,
    /// so the recovered defaults select the English catalog.
    /// </summary>
    public void ReportSettingsRecovery(string? backupFileName) =>
        ReportStatus(backupFileName is null
            ? _text[LocalizationKeys.StatusSettingsRecoveredNoBackup]
            : _text.Format(LocalizationKeys.StatusSettingsRecovered, backupFileName));

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
            Status = _text.Format(LocalizationKeys.StatusSaveFailed, ex.Message);
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

    private void ApplyUiLanguage()
    {
        var oldReady = _text[LocalizationKeys.CaptionReady];
        var choice = LocaleResolver.ResolveUi(_settings.UiLanguage);

        _text = LocalizationCatalog.CreateUiText(choice.Locale, choice.RequestedLocale);
        CultureScope.Apply(_text.Culture);

        Strings = new UiStrings(_text);
        OnPropertyChanged(nameof(Strings));
        OnPropertyChanged(nameof(Text));
        OnPropertyChanged(nameof(FlowDirection));
        OnPropertyChanged(nameof(WpfLanguage));
        OnPropertyChanged(nameof(MuteTooltip));
        OnPropertyChanged(nameof(UiLanguageSetting));
        OnPropertyChanged(nameof(FollowsSystemLanguage));
        OnPropertyChanged(nameof(SystemLanguageName));

        ThemeDisplayName = _text[ThemeCatalog.Get(ThemeId).NameKey];

        if (string.Equals(_caption, oldReady, StringComparison.Ordinal))
        {
            Caption = _text[LocalizationKeys.CaptionReady];
        }

        UpdateLiveTexts();
    }

    private void ApplySpeechLanguage()
    {
        var choice = LocaleResolver.ResolveSpeech(_settings.SpeechLanguage, _text.Locale);

        _formatter = TimeFormatterFactory.Resolve(choice.Locale);
        _catalog = PhraseCatalog.Load(PhraseCatalog.DefaultDirectory, choice.Locale);
        _selector = new PhraseSelector(_catalog.Phrases);

        OnPropertyChanged(nameof(AnnouncementLocale));
        OnPropertyChanged(nameof(AnnouncementLanguageName));
        OnPropertyChanged(nameof(HasVoiceForLanguage));
        OnPropertyChanged(nameof(AutomaticVoice));
        OnPropertyChanged(nameof(EffectiveVoice));
        OnPropertyChanged(nameof(SpeechLanguageSetting));
        OnPropertyChanged(nameof(SpeechFollowsUi));
    }

    /// <summary>
    /// Language and speech notes shown in the status line: a missing UI
    /// translation, missing time grammar, no installed voice for the language,
    /// or an empty phrase catalog. Used at startup and after a language change.
    /// </summary>
    private List<string> BuildLanguageNotes()
    {
        var notes = new List<string>();

        if (_catalog.Diagnostics.Count > 0)
        {
            notes.Add(_catalog.Diagnostics[0]);
        }

        if (_text.UsedFallback)
        {
            notes.Add(_text.Format(LocalizationKeys.StatusUiFallback, _text.RequestedLocale));
        }

        if (_formatter.UsedFallback)
        {
            notes.Add(_text.Format(
                LocalizationKeys.StatusGrammarFallback,
                LocaleRegistry.GetDisplayName(_formatter.Locale)));
        }

        if (!_speech.IsAvailable)
        {
            notes.Add(_text[LocalizationKeys.StatusNoVoice]);
        }
        else if (_speech.Voices.Count == 0)
        {
            notes.Add(_text[LocalizationKeys.StatusNoVoicesInstalled]);
        }
        else if (!HasVoiceForLanguage)
        {
            notes.Add(_text.Format(
                LocalizationKeys.TrayNoVoiceForLanguage,
                AnnouncementLanguageName));
        }

        if (_catalog.Phrases.Count == 0)
        {
            notes.Add(_text[LocalizationKeys.StatusNoPhrases]);
        }

        return notes;
    }

    private string CurrentLanguageStatus()
    {
        var notes = BuildLanguageNotes();
        return notes.Count > 0 ? string.Join(" ", notes) : IntervalStatusText();
    }

    private string IntervalStatusText() => Interval == AnnouncementInterval.Minutes45
        ? _text[LocalizationKeys.StatusInterval45]
        : _text.Format(LocalizationKeys.StatusInterval, (int)Interval);

    /// <summary>
    /// An automatic voice is passed explicitly rather than left null, because a
    /// null name would keep whatever voice SAPI last used instead of resetting to
    /// the Windows default or following the announcement language.
    /// </summary>
    private SpeechOptions CurrentSpeechOptions =>
        new(_settings.SpeechRate, _settings.SpeechVolume, EffectiveVoice);

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
        ThemeDisplayName = _text[theme.NameKey];
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
            NextAnnouncementText = _text[LocalizationKeys.StatusNoAnnouncement];
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
        var timeText = _formatter.Formatter.Format(local, new TimeFormatOptions(_settings.UseNaturalTime, _settings.Use24Hour));
        var joke = TrySelectJoke(local);
        var utterance = joke is null ? timeText : $"{timeText} {joke}";

        Caption = utterance;
        Status = _text.Format(LocalizationKeys.StatusSpokenAt, local.ToString("HH:mm", _text.Culture));
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
            : _text[LocalizationKeys.StatusNoAnnouncement];
    }

    private string BuildNextAnnouncementText(DateTimeOffset next)
    {
        var local = next.ToLocalTime();
        var time = _settings.Use24Hour
            ? local.ToString("HH:mm", _text.Culture)
            : local.ToString("h:mm tt", _text.Culture);
        var minutes = (int)Math.Ceiling((next - _clock.Now).TotalMinutes);
        return minutes <= 0
            ? _text.Format(LocalizationKeys.NextNow, time)
            : _text.Format(LocalizationKeys.NextRelative, time, minutes);
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
        Dispatch(() => Status = _text.Format(LocalizationKeys.StatusSpeechError, message));

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
