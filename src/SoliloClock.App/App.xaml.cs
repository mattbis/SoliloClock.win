using System;
using System.Threading;
using System.Windows;
using SoliloClock.Services;
using SoliloClock.ViewModels;

namespace SoliloClock;

public partial class App : Application
{
    private const string MutexName = @"Local\SoliloClock.SingleInstance";
    private const string ShowEventName = @"Local\SoliloClock.ShowWindow";

    private Mutex? _mutex;
    private EventWaitHandle? _showEvent;
    private Thread? _showListener;
    private MainViewModel? _viewModel;
    private MainWindow? _window;
    private TrayIconService? _trayIcon;
    private volatile bool _isExiting;

    public bool IsExiting => _isExiting;

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        _mutex = new Mutex(initiallyOwned: true, MutexName, out var createdNew);
        if (!createdNew)
        {
            SignalExistingInstance();
            Shutdown();
            return;
        }

        _showEvent = new EventWaitHandle(false, EventResetMode.AutoReset, ShowEventName);
        _showListener = new Thread(ShowListenerLoop)
        {
            IsBackground = true,
            Name = "Talking Clock show listener",
        };
        _showListener.Start();

        var settingsService = new SettingsService();
        var loadResult = settingsService.Load();
        var clock = new SystemClock();
        var speech = new SapiSpeechEngine();
        var catalog = PhraseCatalog.LoadDefault();
        var selector = new PhraseSelector(catalog.Phrases);

        _viewModel = new MainViewModel(settingsService, loadResult.Settings, speech, selector, clock, catalog);
        _viewModel.NotifyRequested += OnNotifyRequested;

        _window = new MainWindow
        {
            DataContext = _viewModel,
        };
        MainWindow = _window;

        _trayIcon = new TrayIconService(_viewModel, _window);

        _window.Show();
        _viewModel.Start();

        if (loadResult.Recovered && loadResult.Message is not null)
        {
            _viewModel.ReportStatus(loadResult.Message);
        }
    }

    public void ExitApplication()
    {
        if (_isExiting)
        {
            return;
        }

        _isExiting = true;

        _viewModel?.Shutdown();
        _trayIcon?.Dispose();
        _trayIcon = null;

        _showEvent?.Set();
        _showListener?.Join(300);

        _window?.CloseForExit();
        Shutdown();
    }

    protected override void OnExit(ExitEventArgs e)
    {
        _isExiting = true;

        _viewModel?.Dispose();
        _trayIcon?.Dispose();
        _trayIcon = null;

        _showEvent?.Set();
        _showListener?.Join(300);
        _showEvent?.Dispose();
        _showEvent = null;

        try
        {
            _mutex?.ReleaseMutex();
        }
        catch (ApplicationException)
        {
            // This process only observed an existing instance; nothing was owned.
        }
        finally
        {
            _mutex?.Dispose();
            _mutex = null;
        }

        base.OnExit(e);
    }

    private void ShowListenerLoop()
    {
        while (!_isExiting)
        {
            var handle = _showEvent;
            if (handle is null)
            {
                return;
            }

            try
            {
                if (!handle.WaitOne(500))
                {
                    continue;
                }
            }
            catch (ObjectDisposedException)
            {
                return;
            }

            if (_isExiting)
            {
                return;
            }

            Dispatcher.BeginInvoke(new Action(() => _window?.ShowFromTray()));
        }
    }

    private static void SignalExistingInstance()
    {
        for (var attempt = 0; attempt < 10; attempt++)
        {
            try
            {
                using var handle = EventWaitHandle.OpenExisting(ShowEventName);
                handle.Set();
                return;
            }
            catch (WaitHandleCannotBeOpenedException)
            {
                Thread.Sleep(100);
            }
            catch (UnauthorizedAccessException)
            {
                return;
            }
        }
    }

    private void OnNotifyRequested(object? sender, Notification notification) =>
        _trayIcon?.ShowBalloon(notification.Title, notification.Message);
}
