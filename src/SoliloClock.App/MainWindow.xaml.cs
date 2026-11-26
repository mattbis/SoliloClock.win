using System;
using System.ComponentModel;
using System.Windows;
using System.Windows.Media.Animation;
using SoliloClock.Services;
using SoliloClock.ViewModels;

namespace SoliloClock;

public partial class MainWindow : Window
{
    private bool _allowClose;

    public MainWindow()
    {
        InitializeComponent();
        DataContextChanged += OnDataContextChanged;
        IsVisibleChanged += (_, _) => (DataContext as MainViewModel)?.SetWindowVisible(IsVisible);
    }

    public void ShowFromTray()
    {
        if (WindowState == WindowState.Minimized)
        {
            WindowState = WindowState.Normal;
        }

        Show();
        Activate();
        (DataContext as MainViewModel)?.SetWindowVisible(true);
    }

    public void CloseForExit()
    {
        _allowClose = true;
        Close();
    }

    private void OnDataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
    {
        if (e.OldValue is MainViewModel oldViewModel)
        {
            oldViewModel.PropertyChanged -= OnViewModelPropertyChanged;
        }

        if (e.NewValue is MainViewModel newViewModel)
        {
            newViewModel.PropertyChanged += OnViewModelPropertyChanged;
        }
    }

    private void OnViewModelPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(MainViewModel.Caption))
        {
            FadeCaption();
        }
    }

    private void FadeCaption()
    {
        CaptionText.BeginAnimation(UIElement.OpacityProperty, null);
        CaptionText.Opacity = 0;
        CaptionText.BeginAnimation(
            UIElement.OpacityProperty,
            new DoubleAnimation(0, 1, TimeSpan.FromMilliseconds(320)));
    }

    private void Minimize_Click(object sender, RoutedEventArgs e)
    {
        Hide();
        (DataContext as MainViewModel)?.SetWindowVisible(false);
    }

    private void Close_Click(object sender, RoutedEventArgs e) => Close();

    protected override void OnClosing(CancelEventArgs e)
    {
        if (!_allowClose)
        {
            e.Cancel = true;
            Hide();

            var viewModel = DataContext as MainViewModel;
            viewModel?.SetWindowVisible(false);

            if (viewModel is { ShouldExplainClose: true })
            {
                viewModel.MarkCloseToTrayExplained();
                MessageBox.Show(
                    viewModel.Text[LocalizationKeys.CloseToTrayMessage],
                    viewModel.Strings.AppName,
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);
            }

            return;
        }

        base.OnClosing(e);
    }

    protected override void OnStateChanged(EventArgs e)
    {
        base.OnStateChanged(e);

        if (WindowState == WindowState.Minimized && IsVisible)
        {
            Hide();
            (DataContext as MainViewModel)?.SetWindowVisible(false);
        }
    }

    private void TitleBar_MouseLeftButtonDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
    {
        if (e.ButtonState != System.Windows.Input.MouseButtonState.Pressed)
        {
            return;
        }

        try
        {
            DragMove();
        }
        catch (InvalidOperationException)
        {
            // DragMove can race window activation; nothing to recover.
        }
    }
}
