using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using SoliloClock.Models;
using SoliloClock.Services;
using SoliloClock.ViewModels;
using Xunit;

namespace SoliloClock.Tests;

/// <summary>
/// Switches the real <see cref="MainViewModel"/> between languages the way the
/// tray menu does, and checks what the user actually sees and hears afterwards:
/// chrome, status, culture, grammar and voice. No dispatcher is running, so the
/// timers are inert; the language path itself is synchronous.
/// </summary>
public sealed class LiveLanguageSwitchingTests : IDisposable
{
    private static readonly DateTimeOffset Startup = new(2026, 11, 26, 9, 0, 0, TimeSpan.Zero);

    private readonly CultureInfo _originalCulture = CultureInfo.CurrentCulture;
    private readonly CultureInfo _originalUiCulture = CultureInfo.CurrentUICulture;
    private readonly string _directory;
    private readonly FakeSpeechEngine _speech = new();
    private readonly MainViewModel _viewModel;

    public LiveLanguageSwitchingTests()
    {
        _directory = Path.Combine(Path.GetTempPath(), "soliloclock-live-" + Guid.NewGuid().ToString("N"));

        var text = LocalizationCatalog.CreateUiText("en-GB");
        var catalog = PhraseCatalog.Load(PhraseDirectory, "en-GB");

        _viewModel = new MainViewModel(
            new SettingsService(_directory),
            new AppSettings(),
            _speech,
            new PhraseSelector(catalog.Phrases),
            new FakeClock(Startup),
            catalog,
            text,
            TimeFormatterFactory.Resolve(text.Locale));
    }

    private static string PhraseDirectory => Path.Combine(AppContext.BaseDirectory, "Phrases");

    [Fact]
    public void SwitchingToWelsh_UpdatesChromeStatusCultureGrammarAndVoiceAtOnce()
    {
        var welsh = LocalizationCatalog.CreateUiText("cy-GB");

        _viewModel.SetUiLanguage("cy-GB");

        Assert.Equal("cy-GB", _viewModel.Text.Locale);
        Assert.Equal("cy-GB", _viewModel.Text.Culture.Name);
        Assert.Equal("cy-GB", CultureInfo.CurrentCulture.Name);
        Assert.Equal("cy-GB", CultureInfo.CurrentUICulture.Name);
        Assert.Equal("cy-GB", _viewModel.WpfLanguage.IetfLanguageTag, ignoreCase: true);
        Assert.False(_viewModel.Text.IsRightToLeft);

        Assert.Equal(welsh[LocalizationKeys.ActionSpeakNow], _viewModel.Strings.SpeakNow);
        Assert.Equal(welsh[LocalizationKeys.Mute], _viewModel.MuteTooltip);
        Assert.Equal(welsh[LocalizationKeys.CaptionReady], _viewModel.Caption);
        Assert.Equal(welsh[LocalizationKeys.StatusNoAnnouncement], _viewModel.NextAnnouncementText);
        Assert.Equal(welsh[ThemeCatalog.Get("pink").NameKey], _viewModel.ThemeDisplayName);
        Assert.DoesNotContain(
            _viewModel.Strings.SpeakNow,
            LocalizationCatalog.CreateUiText("en-GB")[LocalizationKeys.ActionSpeakNow],
            StringComparison.OrdinalIgnoreCase);

        Assert.False(_viewModel.FollowsSystemLanguage);
        Assert.True(_viewModel.SpeechFollowsUi);
        Assert.Equal("cy-GB", _viewModel.UiLanguageSetting);
        Assert.Equal("cy-GB", _viewModel.AnnouncementLocale);
        Assert.Equal(LocaleRegistry.GetDisplayName("cy-GB"), _viewModel.AnnouncementLanguageName);
        Assert.True(_viewModel.HasVoiceForLanguage);
        Assert.Equal("Techiaith Bangor", _viewModel.AutomaticVoice);
        Assert.Equal("Techiaith Bangor", _viewModel.EffectiveVoice);

        _viewModel.SpeakNow();

        Assert.StartsWith("Mae'n ", _speech.LastUtterance, StringComparison.Ordinal);
        Assert.Equal(welsh[LocalizationKeys.StatusSpeakingNow], _viewModel.Status);
        Assert.StartsWith("Mae'n ", _viewModel.Caption, StringComparison.Ordinal);
    }

    [Fact]
    public void SwitchingBackToAutomatic_FollowsTheWindowsLanguageAgain()
    {
        _viewModel.SetUiLanguage("fr-FR");

        Assert.Equal("fr-FR", _viewModel.Text.Locale);
        Assert.Equal("fr-FR", CultureInfo.CurrentCulture.Name);
        Assert.False(_viewModel.FollowsSystemLanguage);

        _viewModel.SpeakNow();
        Assert.StartsWith("Il est ", _speech.LastUtterance, StringComparison.Ordinal);

        _viewModel.SetUiLanguage(null);

        var automatic = LocaleResolver.ResolveUi(null);
        var automaticText = LocalizationCatalog.CreateUiText(automatic.Locale);

        Assert.True(_viewModel.FollowsSystemLanguage);
        Assert.Null(_viewModel.UiLanguageSetting);
        Assert.Equal(LocaleResolver.SystemLocale, _viewModel.Text.RequestedLocale);
        Assert.Equal(automatic.Locale, _viewModel.Text.Locale);
        Assert.Equal(automatic.Locale, _viewModel.AnnouncementLocale);
        Assert.Equal(automaticText[LocalizationKeys.ActionSpeakNow], _viewModel.Strings.SpeakNow);

        // The caption keeps the last utterance across a language switch, so it
        // still reads as the French announcement that was spoken above.
        Assert.StartsWith("Il est ", _viewModel.Caption, StringComparison.Ordinal);

        _viewModel.SpeakNow();
        Assert.False(string.IsNullOrWhiteSpace(_speech.LastUtterance));
    }

    [Fact]
    public void PinningAnnouncementLanguage_KeepsTheInterfaceLanguageAndSurvivesAnInterfaceSwitch()
    {
        var english = LocalizationCatalog.CreateUiText("en-GB");

        _viewModel.SetUiLanguage("en-GB");
        _viewModel.SetSpeechLanguage("cy-GB");

        Assert.Equal("en-GB", _viewModel.Text.Locale);
        Assert.Equal("cy-GB", _viewModel.AnnouncementLocale);
        Assert.Equal("cy-GB", _viewModel.SpeechLanguageSetting);
        Assert.False(_viewModel.SpeechFollowsUi);
        Assert.Equal("Cymraeg", _viewModel.AnnouncementLanguageName);

        _viewModel.SpeakNow();

        Assert.StartsWith("Mae'n ", _speech.LastUtterance, StringComparison.Ordinal);
        Assert.Equal(english[LocalizationKeys.StatusSpeakingNow], _viewModel.Status);

        // A pinned announcement language is a deliberate choice, so an interface
        // switch must not drag speech back to the interface language.
        _viewModel.SetUiLanguage("fr-FR");

        Assert.Equal("fr-FR", _viewModel.Text.Locale);
        Assert.Equal("cy-GB", _viewModel.AnnouncementLocale);
        Assert.Equal("cy-GB", _viewModel.SpeechLanguageSetting);

        _viewModel.SpeakNow();
        Assert.StartsWith("Mae'n ", _speech.LastUtterance, StringComparison.Ordinal);

        _viewModel.SetSpeechLanguage(null);

        Assert.True(_viewModel.SpeechFollowsUi);
        Assert.Equal("fr-FR", _viewModel.AnnouncementLocale);
        Assert.Null(_viewModel.SpeechLanguageSetting);
    }

    [Fact]
    public void LanguageWithoutAnInstalledVoice_SaysSoInsteadOfStayingSilent()
    {
        var french = LocalizationCatalog.CreateUiText("fr-FR");

        _viewModel.SetUiLanguage("fr-FR");

        Assert.False(_viewModel.HasVoiceForLanguage);
        Assert.Null(_viewModel.AutomaticVoice);
        Assert.Null(_viewModel.EffectiveVoice);
        Assert.Equal(
            french.Format(
                LocalizationKeys.TrayNoVoiceForLanguage,
                LocaleRegistry.GetDisplayName("fr-FR")),
            _viewModel.Status);

        _viewModel.SpeakNow();

        Assert.Equal(french[LocalizationKeys.StatusSpeakingNow], _viewModel.Status);
        Assert.StartsWith("Il est ", _speech.LastUtterance, StringComparison.Ordinal);
    }

    public void Dispose()
    {
        _viewModel.Dispose();
        CultureScope.Apply(_originalCulture);
        CultureInfo.DefaultThreadCurrentUICulture = _originalUiCulture;

        try
        {
            Directory.Delete(_directory, recursive: true);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            // A leftover temp directory must not fail the test run.
        }
    }

    private sealed class FakeSpeechEngine : ISpeechEngine
    {
        public bool IsAvailable => true;

        public string? UnavailableReason => null;

        public IReadOnlyList<VoiceOption> Voices { get; } = new[]
        {
            new VoiceOption("Microsoft Hazel Desktop", "en-GB"),
            new VoiceOption("Techiaith Bangor", "cy-GB"),
        };

        public string? SelectedVoice { get; private set; }

        public string? LastUtterance { get; private set; }

        // The tests never simulate a speech failure.
        public event EventHandler<string>? SpeechFailed
        {
            add { }
            remove { }
        }

        public bool TrySelectVoice(string? voiceName)
        {
            SelectedVoice = voiceName;
            return true;
        }

        public void Speak(string text, SpeechOptions options) => LastUtterance = text;

        public void Stop()
        {
        }

        public void Dispose()
        {
        }
    }
}
