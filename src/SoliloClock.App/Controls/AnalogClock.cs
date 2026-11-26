using System;
using System.Globalization;
using System.Windows;
using System.Windows.Media;
using System.Windows.Threading;

namespace SoliloClock.Controls;

/// <summary>
/// Vector analog clock. CompositionTarget.Rendering is attached only while the
/// element is loaded and visible; reduced motion uses a one-second timer
/// instead, and a hidden clock draws nothing at all.
/// </summary>
public sealed class AnalogClock : FrameworkElement
{
    public static readonly DependencyProperty FaceBrushProperty =
        Register(nameof(FaceBrush), (Brush?)Brushes.Transparent);

    public static readonly DependencyProperty RimBrushProperty =
        Register(nameof(RimBrush), (Brush?)Brushes.Gray);

    public static readonly DependencyProperty TickMajorBrushProperty =
        Register(nameof(TickMajorBrush), (Brush?)Brushes.Gray);

    public static readonly DependencyProperty TickMinorBrushProperty =
        Register(nameof(TickMinorBrush), (Brush?)Brushes.LightGray);

    public static readonly DependencyProperty HourHandBrushProperty =
        Register(nameof(HourHandBrush), (Brush?)Brushes.Black);

    public static readonly DependencyProperty MinuteHandBrushProperty =
        Register(nameof(MinuteHandBrush), (Brush?)Brushes.Black);

    public static readonly DependencyProperty SecondHandBrushProperty =
        Register(nameof(SecondHandBrush), (Brush?)Brushes.Crimson);

    public static readonly DependencyProperty AccentBrushProperty =
        Register(nameof(AccentBrush), (Brush?)Brushes.Crimson);

    public static readonly DependencyProperty GlowBrushProperty =
        Register(nameof(GlowBrush), (Brush?)Brushes.Transparent);

    public static readonly DependencyProperty NumeralBrushProperty =
        Register(nameof(NumeralBrush), (Brush?)Brushes.Black);

    public static readonly DependencyProperty FontFamilyProperty =
        Register(nameof(FontFamily), new FontFamily("Segoe UI"));

    public static readonly DependencyProperty ReducedMotionProperty =
        Register(nameof(ReducedMotion), false);

    public static readonly DependencyProperty SparklesProperty =
        Register(nameof(Sparkles), false);

    public static readonly DependencyProperty GlowProperty =
        Register(nameof(Glow), false);

    private readonly DispatcherTimer _stillTimer;
    private bool _renderingAttached;

    public AnalogClock()
    {
        _stillTimer = new DispatcherTimer(DispatcherPriority.Render)
        {
            Interval = TimeSpan.FromSeconds(1),
        };
        _stillTimer.Tick += (_, _) => InvalidateVisual();

        Loaded += (_, _) => UpdateRenderingLoop();
        Unloaded += (_, _) =>
        {
            DetachRenderingLoop();
            _stillTimer.Stop();
        };
        IsVisibleChanged += (_, _) => UpdateRenderingLoop();
    }

    public Brush? FaceBrush
    {
        get => (Brush?)GetValue(FaceBrushProperty);
        set => SetValue(FaceBrushProperty, value);
    }

    public Brush? RimBrush
    {
        get => (Brush?)GetValue(RimBrushProperty);
        set => SetValue(RimBrushProperty, value);
    }

    public Brush? TickMajorBrush
    {
        get => (Brush?)GetValue(TickMajorBrushProperty);
        set => SetValue(TickMajorBrushProperty, value);
    }

    public Brush? TickMinorBrush
    {
        get => (Brush?)GetValue(TickMinorBrushProperty);
        set => SetValue(TickMinorBrushProperty, value);
    }

    public Brush? HourHandBrush
    {
        get => (Brush?)GetValue(HourHandBrushProperty);
        set => SetValue(HourHandBrushProperty, value);
    }

    public Brush? MinuteHandBrush
    {
        get => (Brush?)GetValue(MinuteHandBrushProperty);
        set => SetValue(MinuteHandBrushProperty, value);
    }

    public Brush? SecondHandBrush
    {
        get => (Brush?)GetValue(SecondHandBrushProperty);
        set => SetValue(SecondHandBrushProperty, value);
    }

    public Brush? AccentBrush
    {
        get => (Brush?)GetValue(AccentBrushProperty);
        set => SetValue(AccentBrushProperty, value);
    }

    public Brush? GlowBrush
    {
        get => (Brush?)GetValue(GlowBrushProperty);
        set => SetValue(GlowBrushProperty, value);
    }

    public Brush? NumeralBrush
    {
        get => (Brush?)GetValue(NumeralBrushProperty);
        set => SetValue(NumeralBrushProperty, value);
    }

    public FontFamily FontFamily
    {
        get => (FontFamily)GetValue(FontFamilyProperty);
        set => SetValue(FontFamilyProperty, value);
    }

    public bool ReducedMotion
    {
        get => (bool)GetValue(ReducedMotionProperty);
        set => SetValue(ReducedMotionProperty, value);
    }

    public bool Sparkles
    {
        get => (bool)GetValue(SparklesProperty);
        set => SetValue(SparklesProperty, value);
    }

    public bool Glow
    {
        get => (bool)GetValue(GlowProperty);
        set => SetValue(GlowProperty, value);
    }

    protected override void OnRender(DrawingContext drawingContext)
    {
        base.OnRender(drawingContext);

        var width = ActualWidth;
        var height = ActualHeight;
        if (width <= 2 || height <= 2)
        {
            return;
        }

        var now = DateTime.Now;
        var size = Math.Min(width, height);
        var center = new Point(width / 2, height / 2);
        var radius = Math.Max(12, size / 2 - 8);
        var pixelsPerDip = VisualTreeHelper.GetDpi(this).PixelsPerDip;

        if (Glow && GlowBrush is not null)
        {
            DrawGlow(drawingContext, center, radius, now);
        }

        drawingContext.DrawEllipse(
            FaceBrush,
            RimBrush is null ? null : new Pen(RimBrush, 1.5),
            center,
            radius,
            radius);

        if (TickMinorBrush is not null)
        {
            drawingContext.DrawEllipse(
                null,
                new Pen(TickMinorBrush, 1),
                center,
                radius - 3,
                radius - 3);
        }

        DrawTicks(drawingContext, center, radius);

        if (NumeralBrush is not null && radius > 60)
        {
            DrawNumerals(drawingContext, center, radius, pixelsPerDip);
        }

        var seconds = now.Second + (ReducedMotion ? 0 : now.Millisecond / 1000.0);
        var minutes = now.Minute + now.Second / 60.0;
        var hours = now.Hour % 12 + now.Minute / 60.0;

        DrawHand(drawingContext, center, hours * 30, radius * 0.47, Math.Max(3, radius * 0.045), HourHandBrush, radius * 0.06);
        DrawHand(drawingContext, center, minutes * 6, radius * 0.72, Math.Max(2, radius * 0.03), MinuteHandBrush, radius * 0.08);
        DrawHand(drawingContext, center, seconds * 6, radius * 0.82, Math.Max(1.2, radius * 0.012), SecondHandBrush, radius * 0.16);

        if (SecondHandBrush is not null)
        {
            drawingContext.DrawEllipse(SecondHandBrush, null, center, Math.Max(3, radius * 0.04), Math.Max(3, radius * 0.04));
        }

        if (FaceBrush is not null)
        {
            drawingContext.DrawEllipse(FaceBrush, null, center, Math.Max(1.5, radius * 0.018), Math.Max(1.5, radius * 0.018));
        }

        if (Sparkles && AccentBrush is not null)
        {
            DrawSparkles(drawingContext, center, radius);
        }
    }

    private void DrawTicks(DrawingContext drawingContext, Point center, double radius)
    {
        for (var i = 0; i < 60; i++)
        {
            var major = i % 5 == 0;
            var brush = major ? TickMajorBrush : TickMinorBrush;
            if (brush is null)
            {
                continue;
            }

            var outer = radius - 5;
            var inner = major ? radius - 17 : radius - 11;
            var pen = new Pen(brush, major ? 2.0 : 1.0);
            drawingContext.DrawLine(
                pen,
                PointAt(center, i * 6, inner),
                PointAt(center, i * 6, outer));
        }
    }

    private void DrawNumerals(DrawingContext drawingContext, Point center, double radius, double pixelsPerDip)
    {
        if (NumeralBrush is null)
        {
            return;
        }

        var emSize = Math.Max(10, radius * 0.13);
        var typeface = new Typeface(FontFamily, FontStyles.Normal, FontWeights.Normal, FontStretches.Normal);
        var culture = NumeralsCulture;

        for (var hour = 1; hour <= 12; hour++)
        {
            var text = hour.ToString(culture);
            var formatted = new FormattedText(
                text,
                culture,
                FlowDirection,
                typeface,
                emSize,
                NumeralBrush,
                pixelsPerDip);

            var position = PointAt(center, hour * 30, radius - 30 - emSize);
            drawingContext.DrawText(
                formatted,
                new Point(position.X - formatted.Width / 2, position.Y - formatted.Height / 2));
        }
    }

    /// <summary>Culture inherited from the window language, so numerals and layout match the UI.</summary>
    private CultureInfo NumeralsCulture
    {
        get
        {
            try
            {
                if (Language is not null)
                {
                    return Language.GetSpecificCulture();
                }
            }
            catch (InvalidOperationException)
            {
            }
            catch (ArgumentException)
            {
            }

            return CultureInfo.CurrentCulture;
        }
    }

    private void DrawGlow(DrawingContext drawingContext, Point center, double radius, DateTime now)
    {
        var pulse = 0.5 + 0.5 * Math.Sin(now.TimeOfDay.TotalSeconds * Math.PI / 3);

        for (var ring = 1; ring <= 3; ring++)
        {
            drawingContext.PushOpacity((0.05 + 0.05 * pulse) * (4 - ring));
            drawingContext.DrawEllipse(
                null,
                new Pen(GlowBrush, 3 + ring * 3),
                center,
                radius + ring * 4,
                radius + ring * 4);
            drawingContext.Pop();
        }
    }

    private void DrawSparkles(DrawingContext drawingContext, Point center, double radius)
    {
        if (AccentBrush is null)
        {
            return;
        }

        drawingContext.PushOpacity(0.55);
        DrawHeart(drawingContext, new Point(center.X - radius * 0.58, center.Y - radius * 0.44), radius * 0.06, AccentBrush);
        DrawHeart(drawingContext, new Point(center.X + radius * 0.62, center.Y + radius * 0.08), radius * 0.045, AccentBrush);
        DrawHeart(drawingContext, new Point(center.X + radius * 0.22, center.Y - radius * 0.74), radius * 0.035, AccentBrush);
        drawingContext.Pop();
    }

    private static void DrawHeart(DrawingContext drawingContext, Point center, double size, Brush brush)
    {
        var geometry = new StreamGeometry();
        using (var context = geometry.Open())
        {
            context.BeginFigure(new Point(0, 0.9), isFilled: true, isClosed: true);
            context.BezierTo(new Point(-1.15, 0.1), new Point(-1.1, -0.9), new Point(-0.5, -0.9), true, false);
            context.BezierTo(new Point(-0.15, -0.9), new Point(0, -0.55), new Point(0, -0.4), true, false);
            context.BezierTo(new Point(0, -0.55), new Point(0.15, -0.9), new Point(0.5, -0.9), true, false);
            context.BezierTo(new Point(1.1, -0.9), new Point(1.15, 0.1), new Point(0, 0.9), true, false);
        }

        geometry.Freeze();

        drawingContext.PushTransform(new TranslateTransform(center.X, center.Y));
        drawingContext.PushTransform(new ScaleTransform(size, size));
        drawingContext.DrawGeometry(brush, null, geometry);
        drawingContext.Pop();
        drawingContext.Pop();
    }

    private static void DrawHand(
        DrawingContext drawingContext,
        Point center,
        double angle,
        double length,
        double thickness,
        Brush? brush,
        double tail)
    {
        if (brush is null)
        {
            return;
        }

        var pen = new Pen(brush, thickness)
        {
            StartLineCap = PenLineCap.Round,
            EndLineCap = PenLineCap.Round,
        };

        var start = tail > 0 ? PointAt(center, angle + 180, tail) : center;
        drawingContext.DrawLine(pen, start, PointAt(center, angle, length));
    }

    private static Point PointAt(Point center, double angleDegrees, double radius)
    {
        var radians = (angleDegrees - 90) * Math.PI / 180.0;
        return new Point(
            center.X + Math.Cos(radians) * radius,
            center.Y + Math.Sin(radians) * radius);
    }

    private void UpdateRenderingLoop()
    {
        var shouldDraw = IsLoaded && IsVisible;
        var smooth = shouldDraw && !ReducedMotion;

        if (smooth && !_renderingAttached)
        {
            CompositionTarget.Rendering += OnRenderingFrame;
            _renderingAttached = true;
        }
        else if (!smooth)
        {
            DetachRenderingLoop();
        }

        if (shouldDraw && ReducedMotion)
        {
            _stillTimer.Start();
        }
        else
        {
            _stillTimer.Stop();
        }

        InvalidateVisual();
    }

    private void DetachRenderingLoop()
    {
        if (!_renderingAttached)
        {
            return;
        }

        CompositionTarget.Rendering -= OnRenderingFrame;
        _renderingAttached = false;
    }

    private void OnRenderingFrame(object? sender, EventArgs e)
    {
        if (IsVisible)
        {
            InvalidateVisual();
        }
    }

    private static DependencyProperty Register<T>(string name, T defaultValue)
    {
        return DependencyProperty.Register(
            name,
            typeof(T),
            typeof(AnalogClock),
            new FrameworkPropertyMetadata(
                defaultValue,
                FrameworkPropertyMetadataOptions.AffectsRender,
                OnVisualPropertyChanged));
    }

    private static void OnVisualPropertyChanged(DependencyObject dependencyObject, DependencyPropertyChangedEventArgs e)
    {
        if (dependencyObject is not AnalogClock clock)
        {
            return;
        }

        if (e.Property == ReducedMotionProperty)
        {
            clock.UpdateRenderingLoop();
        }

        clock.InvalidateVisual();
    }
}
