using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Runtime.InteropServices;

namespace SoliloClock.Services;

/// <summary>
/// Draws the tray icon at runtime. Keeping it code-generated avoids a binary
/// asset in source control and guarantees a crisp icon at system DPI.
/// </summary>
public static class AppIconFactory
{
    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool DestroyIcon(IntPtr handle);

    public static Icon Create()
    {
        using var bitmap = new Bitmap(32, 32);
        using (var graphics = Graphics.FromImage(bitmap))
        {
            graphics.SmoothingMode = SmoothingMode.AntiAlias;
            graphics.Clear(Color.Transparent);

            using var face = new SolidBrush(Color.FromArgb(255, 32, 36, 48));
            using var rim = new Pen(Color.FromArgb(255, 255, 122, 173), 2f);
            using var hand = new Pen(Color.FromArgb(255, 244, 246, 250), 2.2f)
            {
                StartCap = LineCap.Round,
                EndCap = LineCap.Round,
            };
            using var center = new SolidBrush(Color.FromArgb(255, 255, 122, 173));

            graphics.FillEllipse(face, 2, 2, 28, 28);
            graphics.DrawEllipse(rim, 2.5f, 2.5f, 27, 27);
            graphics.DrawLine(hand, 16, 16, 16, 9);
            graphics.DrawLine(hand, 16, 16, 22, 19);
            graphics.FillEllipse(center, 14, 14, 4, 4);
        }

        var handle = bitmap.GetHicon();
        try
        {
            using var temporary = Icon.FromHandle(handle);
            return (Icon)temporary.Clone();
        }
        finally
        {
            DestroyIcon(handle);
        }
    }
}
