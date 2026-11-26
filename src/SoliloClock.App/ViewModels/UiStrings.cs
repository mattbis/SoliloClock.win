using SoliloClock.Services;

namespace SoliloClock.ViewModels;

/// <summary>
/// Named, bindable view of the current <see cref="UiText"/>. XAML binds
/// <c>Strings.SpeakNow</c> rather than an indexer, so a renamed key is a compile
/// error instead of a silent blank.
/// </summary>
public sealed class UiStrings
{
    public UiStrings(UiText text)
    {
        AppName = text[LocalizationKeys.AppName];
        BrandTitle = text[LocalizationKeys.AppBrand];
        HideToTray = text[LocalizationKeys.WindowHideToTray];
        CloseToTray = text[LocalizationKeys.WindowCloseToTray];
        IntervalHeading = text[LocalizationKeys.IntervalHeading];
        Interval15Tooltip = text[LocalizationKeys.IntervalTooltip15];
        Interval30Tooltip = text[LocalizationKeys.IntervalTooltip30];
        Interval45Tooltip = text[LocalizationKeys.IntervalTooltip45];
        Interval60Tooltip = text[LocalizationKeys.IntervalTooltip60];
        SpeakNow = text[LocalizationKeys.ActionSpeakNow];
        ChangeTheme = text[LocalizationKeys.ActionChangeTheme];
        AlwaysOnTop = text[LocalizationKeys.ActionAlwaysOnTop];
        WindowOpacity = text[LocalizationKeys.OpacityLabel];
        FooterHint = text[LocalizationKeys.FooterHint];
        AnalogClockName = text[LocalizationKeys.A11yClock];
        LastSpokenName = text[LocalizationKeys.A11yLastSpoken];
        SpeakNowAutomation = text[LocalizationKeys.A11ySpeakNow];
    }

    public string AppName { get; }

    public string BrandTitle { get; }

    public string HideToTray { get; }

    public string CloseToTray { get; }

    public string IntervalHeading { get; }

    public string Interval15Tooltip { get; }

    public string Interval30Tooltip { get; }

    public string Interval45Tooltip { get; }

    public string Interval60Tooltip { get; }

    public string SpeakNow { get; }

    public string ChangeTheme { get; }

    public string AlwaysOnTop { get; }

    public string WindowOpacity { get; }

    public string FooterHint { get; }

    public string AnalogClockName { get; }

    public string LastSpokenName { get; }

    public string SpeakNowAutomation { get; }
}
