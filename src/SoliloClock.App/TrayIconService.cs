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
/// </summary>
public sealed class TrayIconService : IDisposable
{
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
            Text = "Talking Clock",
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

        var menu = new ContextMenuStrip();

        var speak = new ToolStripMenuItem("Speak time now");
        speak.Click += (_, _) => _viewModel.SpeakNow();
        menu.Items.Add(speak);

        menu.Items.Add(new ToolStripSeparator());

        foreach (var interval in AnnouncementIntervals.All)
        {
            var item = new ToolStripMenuItem($"Every {(int)interval} minutes")
            {
                Checked = _viewModel.Interval == interval,
            };
            item.Click += (_, _) => _viewModel.SelectInterval(interval);
            menu.Items.Add(item);
        }

        menu.Items.Add(new ToolStripSeparator());

        var mute = new ToolStripMenuItem(_viewModel.IsMuted ? "Unmute announcements" : "Mute announcements")
        {
            Checked = _viewModel.IsMuted,
        };
        mute.Click += (_, _) => _viewModel.IsMuted = !_viewModel.IsMuted;
        menu.Items.Add(mute);

        var themes = new ToolStripMenuItem("Theme");
        foreach (var theme in ThemeCatalog.All)
        {
            var item = new ToolStripMenuItem(theme.DisplayName)
            {
                Checked = string.Equals(theme.Id, _viewModel.ThemeId, StringComparison.OrdinalIgnoreCase),
                ToolTipText = theme.Description,
            };
            item.Click += (_, _) => _viewModel.SetTheme(theme.Id);
            themes.DropDownItems.Add(item);
        }

        menu.Items.Add(themes);

        var options = new ToolStripMenuItem("Options");
        var reducedMotion = new ToolStripMenuItem("Reduced motion")
        {
            Checked = _viewModel.ReducedMotionSetting,
        };
        reducedMotion.Click += (_, _) => _viewModel.ReducedMotionSetting = !_viewModel.ReducedMotionSetting;
        options.DropDownItems.Add(reducedMotion);

        var alwaysOnTop = new ToolStripMenuItem("Always on top")
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

        var show = new ToolStripMenuItem("Show clock");
        show.Click += (_, _) => ShowClock();
        menu.Items.Add(show);

        var exit = new ToolStripMenuItem("Exit");
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

        _notifyIcon.Text = Truncate(_viewModel.ShortStatus, 120);
    }

    private static string Truncate(string text, int maxLength) =>
        string.IsNullOrEmpty(text) || text.Length <= maxLength
            ? text
            : text[..Math.Max(0, maxLength - 1)] + "…";
}
