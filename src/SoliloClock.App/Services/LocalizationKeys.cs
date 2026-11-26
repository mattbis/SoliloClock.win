namespace SoliloClock.Services;

/// <summary>
/// Every UI string key used by the app. Keeping them in one place lets a test
/// assert that the English catalog covers all of them, so a missing key is a
/// build failure rather than a literal leaking into the interface.
/// </summary>
public static class LocalizationKeys
{
    public const string AppName = "app.name";
    public const string AppBrand = "app.brand";

    public const string WindowHideToTray = "window.hideToTray";
    public const string WindowCloseToTray = "window.closeToTray";

    public const string IntervalHeading = "interval.heading";
    public const string IntervalTooltip15 = "interval.tooltip.15";
    public const string IntervalTooltip30 = "interval.tooltip.30";
    public const string IntervalTooltip45 = "interval.tooltip.45";
    public const string IntervalTooltip60 = "interval.tooltip.60";

    public const string ActionSpeakNow = "action.speakNow";
    public const string ActionChangeTheme = "action.changeTheme";
    public const string ActionAlwaysOnTop = "action.alwaysOnTop";
    public const string OpacityLabel = "opacity.label";
    public const string FooterHint = "footer.hint";

    public const string A11yClock = "a11y.clock";
    public const string A11yLastSpoken = "a11y.lastSpoken";
    public const string A11ySpeakNow = "a11y.speakNow";

    public const string CaptionReady = "caption.ready";
    public const string NextRelative = "next.relative";
    public const string NextNow = "next.now";
    public const string StatusNoAnnouncement = "status.noAnnouncement";
    public const string StatusShort = "status.short";

    public const string Mute = "mute.mute";
    public const string Unmute = "mute.unmute";

    public const string StatusInterval45 = "status.interval45";
    public const string StatusInterval = "status.interval";
    public const string StatusSpeakingNow = "status.speakingNow";
    public const string StatusNoVoiceCaption = "status.noVoiceCaption";
    public const string StatusSpokenAt = "status.spokenAt";
    public const string StatusNoVoice = "status.noVoice";
    public const string StatusNoVoicesInstalled = "status.noVoicesInstalled";
    public const string StatusNoPhrases = "status.noPhrases";
    public const string StatusSavedVoiceMissing = "status.savedVoiceMissing";
    public const string StatusSaveFailed = "status.saveFailed";
    public const string StatusSpeechError = "status.speechError";
    public const string StatusUiFallback = "status.uiFallback";
    public const string StatusGrammarFallback = "status.grammarFallback";
    public const string StatusSettingsRecovered = "status.settingsRecovered";
    public const string StatusSettingsRecoveredNoBackup = "status.settingsRecoveredNoBackup";

    public const string NotifySpeechUnavailableTitle = "notify.speechUnavailable.title";
    public const string NotifySpeechUnavailableMessage = "notify.speechUnavailable.message";

    public const string TraySpeakNow = "tray.speakNow";
    public const string TrayMute = "tray.mute";
    public const string TrayUnmute = "tray.unmute";
    public const string TrayTheme = "tray.theme";
    public const string TrayOptions = "tray.options";
    public const string TrayReducedMotion = "tray.reducedMotion";
    public const string TrayAlwaysOnTop = "tray.alwaysOnTop";
    public const string TrayVoice = "tray.voice";
    public const string TrayVoiceAutomatic = "tray.voiceAutomatic";
    public const string TrayTestVoice = "tray.testVoice";
    public const string TrayNoVoiceForLanguage = "tray.noVoiceForLanguage";
    public const string TrayShow = "tray.show";
    public const string TrayExit = "tray.exit";
    public const string TrayLanguage = "tray.language";
    public const string TrayLanguageAutomatic = "tray.languageAutomatic";
    public const string TrayAnnounceIn = "tray.announceIn";
    public const string TraySpeechSameAsUi = "tray.speechSameAsUi";
    public const string TrayProvisionalSuffix = "tray.provisionalSuffix";

    public const string CloseToTrayMessage = "closeToTray.message";
    public const string VoiceTestSample = "voice.testSample";

    public const string ThemePinkName = "theme.pink.name";
    public const string ThemePinkDescription = "theme.pink.description";
    public const string ThemeBlueName = "theme.blue.name";
    public const string ThemeBlueDescription = "theme.blue.description";
    public const string ThemeBlackName = "theme.black.name";
    public const string ThemeBlackDescription = "theme.black.description";
    public const string ThemeNeonName = "theme.neon.name";
    public const string ThemeNeonDescription = "theme.neon.description";
    public const string ThemeTransparentName = "theme.transparent.name";
    public const string ThemeTransparentDescription = "theme.transparent.description";
}
