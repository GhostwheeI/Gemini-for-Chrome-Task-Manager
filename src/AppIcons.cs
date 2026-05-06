namespace GeminiForChromeManager;

internal static class AppIcons
{
    public static Icon CreateTrayIcon(bool enabled)
    {
        using Bitmap bitmap = new(64, 64);
        using Graphics graphics = Graphics.FromImage(bitmap);

        graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
        graphics.Clear(Color.Transparent);

        Color outer = enabled ? Color.FromArgb(80, 145, 255) : Color.FromArgb(110, 110, 118);
        Color inner = enabled ? Color.FromArgb(37, 208, 138) : Color.FromArgb(175, 175, 182);

        using SolidBrush outerBrush = new(outer);
        using SolidBrush innerBrush = new(inner);
        using Pen whitePen = new(Color.White, 4);
        using Font font = new("Segoe UI", 25, FontStyle.Bold, GraphicsUnit.Pixel);
        using SolidBrush textBrush = new(Color.White);

        Point[] diamond =
        [
            new(32, 4),
            new(60, 32),
            new(32, 60),
            new(4, 32)
        ];

        graphics.FillPolygon(outerBrush, diamond);
        graphics.DrawPolygon(whitePen, diamond);
        graphics.FillEllipse(innerBrush, 19, 19, 26, 26);

        StringFormat format = new()
        {
            Alignment = StringAlignment.Center,
            LineAlignment = StringAlignment.Center
        };

        graphics.DrawString("G", font, textBrush, new RectangleF(0, 0, 64, 64), format);

        IntPtr handle = bitmap.GetHicon();

        try
        {
            using Icon temporary = Icon.FromHandle(handle);
            return (Icon)temporary.Clone();
        }
        finally
        {
            DestroyIcon(handle);
        }
    }

    [System.Runtime.InteropServices.DllImport("user32.dll", SetLastError = true)]
    private static extern bool DestroyIcon(IntPtr hIcon);
}
