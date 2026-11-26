using System;
using System.Drawing;
using System.Windows.Forms;
using SoliloClock.Models;
using SoliloClock.Services;
using SoliloClock.ViewModels;

namespace SoliloClock;

/// <summary>
/// System-tray presence. Owned by the UI thread; the menu is rebuilt whenever
/// the view model reports a state change so checkmarks and labels stay true.
/// Labels come from the active UI language, never from hardcoded literals.
/// </summary>
public sealed class TrayIconService : IDisposable
{
    private const int MaxTooltipLength = 63;

    private readonly MainViewModel _viewModel;
    private readonly MainWindow _window;
    private readonly NotifyIcon _notifyIcon;
    private readonly Icon _icon;
    private bool _disposed;

    public TrayIconService(MainViewModel viewModel, MainWindow window)
    {
        _viewModel = viewModel ?? throw new ArgumentNullException(nameof(viewModel));
        _window = window ?? throw new ArgumentNullException(nameof(window));

        _icon = AppIconFactory.Create();
        _notifyIcon = new NotifyIcon
        {
            Icon = _icon,
            Text = TextTruncation.Truncate(_viewModel.Strings.AppName, MaxTooltipLength),
            Visible = true,
        };

        _notifyIcon.MouseClick += OnMouseClick;
        _viewModel.StateChanged += OnViewModelStateChanged;

        RebuildMenu();
    }

    public void ShowBalloon(string title, string message)
    {
        if (_disposed)
        {
            return;
        }

        try
        {
            _notifyIcon.ShowBalloonTip(5000, Truncate(title, 60), Truncate(message, 250), ToolTipIcon.Info);
        }
        catch (Exception)
        {
            // Balloon tips are best-effort; the status line carries the message too.
        }
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        _viewModel.StateChanged -= OnViewModelStateChanged;
        _notifyIcon.MouseClick -= OnMouseClick;
        _notifyIcon.Visible = false;

        var menu = _notifyIcon.ContextMenuStrip;
        _notifyIcon.ContextMenuStrip = null;
        menu?.Dispose();
        _notifyIcon.Dispose();
        _icon.Dispose();
    }

    private void OnViewModelStateChanged(object? sender, EventArgs e) => RebuildMenu();

    private void OnMouseClick(object? sender, MouseEventArgs e)
    {
        if (e.Button == MouseButtons.Left)
        {
            ShowClock();
        }
    }

    private void ShowClock()
    {
        if (_disposed)
        {
            return;
        }

        _window.ShowFromTray();
    }

    private void RebuildMenu()
    {
        if (_disposed)
        {
            return;
        }

        var text = _viewModel.Text;
        var menu = new ContextMenuStrip();

        var speak = new ToolStripMenuItem(text[LocalizationKeys.TraySpeakNow]);
        speak.Click += (_, _) => _viewModel.SpeakNow();
        menu.Items.Add(speak);

        menu.Items.Add(new ToolStripSeparator());

        foreach (var interval in AnnouncementIntervals.All)
        {
            var item = new ToolStripMenuItem(text[IntervalKey(interval)])
            {
                Checked = _viewModel.Interval == interval,
            };
            item.Click += (_, _) => _viewModel.SelectInterval(interval);
            menu.Items.Add(item);
        }

        menu.Items.Add(new ToolStripSeparator());

        var mute = new ToolStripMenuItem(_viewModel.IsMuted
            ? text[LocalizationKeys.TrayUnmute]
            : text[LocalizationKeys.TrayMute])
        {
            Checked = _viewModel.IsMuted,
        };
        mute.Click += (_, _) => _viewModel.IsMuted = !_viewModel.IsMuted;
        menu.Items.Add(mute);

        var themes = new ToolStripMenuItem(text[LocalizationKeys.TrayTheme]);
        foreach (var theme in ThemeCatalog.All)
        {
            var item = new ToolStripMenuItem(text[theme.NameKey])
            {
                Checked = string.Equals(theme.Id, _viewModel.ThemeId, StringComparison.OrdinalIgnoreCase),
                ToolTipText = text[theme.DescriptionKey],
            };
            item.Click += (_, _) => _viewModel.SetTheme(theme.Id);
            themes.DropDownItems.Add(item);
        }

        menu.Items.Add(themes);

        var options = new ToolStripMenuItem(text[LocalizationKeys.TrayOptions]);
        options.DropDownItems.Add(BuildVoiceMenu(text));

        var reducedMotion = new ToolStripMenuItem(text[LocalizationKeys.TrayReducedMotion])
        {
            Checked = _viewModel.ReducedMotionSetting,
        };
        reducedMotion.Click += (_, _) => _viewModel.ReducedMotionSetting = !_viewModel.ReducedMotionSetting;
        options.DropDownItems.Add(reducedMotion);

        var alwaysOnTop = new ToolStripMenuItem(text[LocalizationKeys.TrayAlwaysOnTop])
        {
            Checked = _viewModel.AlwaysOnTop,
        };
        alwaysOnTop.Click += (_, _) => _viewModel.AlwaysOnTop = !_viewModel.AlwaysOnTop;
        options.DropDownItems.Add(alwaysOnTop);
        menu.Items.Add(options);

        menu.Items.Add(new ToolStripSeparator());

        var next = new ToolStripMenuItem(_viewModel.NextAnnouncementText)
        {
            Enabled = false,
        };
        menu.Items.Add(next);

        var show = new ToolStripMenuItem(text[LocalizationKeys.TrayShow]);
        show.Click += (_, _) => ShowClock();
        menu.Items.Add(show);

        var exit = new ToolStripMenuItem(text[LocalizationKeys.TrayExit]);
        exit.Click += (_, _) =>
        {
            if (System.Windows.Application.Current is App app)
            {
                app.ExitApplication();
            }
        };
        menu.Items.Add(exit);

        var previous = _notifyIcon.ContextMenuStrip;
        _notifyIcon.ContextMenuStrip = menu;
        previous?.Dispose();

        _notifyIcon.Text = TextTruncation.Truncate(_viewModel.ShortStatus, MaxTooltipLength);
    }

    /// <summary>
    /// Lists only voices Windows actually reports, with the culture each one
    /// speaks, plus Test voice. When no installed voice matches the announcement
    /// language the menu says so instead of implying the language can speak.
    /// </summary>
    private ToolStripMenuItem BuildVoiceMenu(UiText text)
    {
        var voice = new ToolStripMenuItem(text[LocalizationKeys.TrayVoice]);
        var voices = _viewModel.Voices;

        var useDefault = new ToolStripMenuItem(text[LocalizationKeys.TrayVoiceDefault])
        {
            Checked = _viewModel.SelectedVoice is null,
        };
        useDefault.Click += (_, _) => _viewModel.SetVoice(null);
        voice.DropDownItems.Add(useDefault);

        if (voices.Count > 0)
        {
            voice.DropDownItems.Add(new ToolStripSeparator());

            foreach (var installed in voices)
            {
                var label = string.IsNullOrWhiteSpace(installed.Culture)
                    ? installed.Name
                    : $"{installed.Name} ({installed.Culture})";

                var item = new ToolStripMenuItem(label)
                {
                    Checked = string.Equals(
                        installed.Name,
                        _viewModel.SelectedVoice,
                        StringComparison.OrdinalIgnoreCase),
                };
                item.Click += (_, _) => _viewModel.SetVoice(installed.Name);
                voice.DropDownItems.Add(item);
            }

            voice.DropDownItems.Add(new ToolStripSeparator());

            var test = new ToolStripMenuItem(text[LocalizationKeys.TrayTestVoice]);
            test.Click += (_, _) => _viewModel.TestVoice();
            voice.DropDownItems.Add(test);
        }

        if (!_viewModel.HasVoiceForLanguage)
        {
            voice.DropDownItems.Add(new ToolStripSeparator());
            voice.DropDownItems.Add(new ToolStripMenuItem(
                text.Format(LocalizationKeys.TrayNoVoiceForLanguage, _viewModel.AnnouncementLanguageName))
            {
                Enabled = false,
            });
        }

        return voice;
    }

    private static string IntervalKey(AnnouncementInterval interval) => (int)interval switch
    {
        15 => LocalizationKeys.IntervalTooltip15,
        30 => LocalizationKeys.IntervalTooltip30,
        45 => LocalizationKeys.IntervalTooltip45,
        _ => LocalizationKeys.IntervalTooltip60,
    };

    private static string Truncate(string text, int maxLength) =>
        TextTruncation.Truncate(text, maxLength);
}
