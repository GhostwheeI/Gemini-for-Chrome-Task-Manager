using System.Runtime.InteropServices;
using System.Text;
using System.Diagnostics;

namespace GeminiForChromeManager;

internal static class ChromeWindowLocator
{
    private delegate bool EnumWindowsProc(IntPtr hwnd, IntPtr lParam);

    private const int SwRestore = 9;

    public static bool IsPointInsideForegroundChromeWindow(Point point, out Rectangle windowBounds, out string windowName)
    {
        IntPtr foregroundWindow = GetForegroundWindow();

        if (foregroundWindow == IntPtr.Zero ||
            !IsWindowVisible(foregroundWindow) ||
            !GetWindowClassName(foregroundWindow).Equals("Chrome_WidgetWin_1", StringComparison.OrdinalIgnoreCase) ||
            !IsOwnedByChrome(foregroundWindow) ||
            !GetWindowRect(foregroundWindow, out NativeRectangle nativeRectangle))
        {
            windowBounds = Rectangle.Empty;
            windowName = string.Empty;
            return false;
        }

        Rectangle bounds = Rectangle.FromLTRB(
            nativeRectangle.Left,
            nativeRectangle.Top,
            nativeRectangle.Right,
            nativeRectangle.Bottom);

        if (bounds.Width < 400 || bounds.Height < 300 || !bounds.Contains(point))
        {
            windowBounds = Rectangle.Empty;
            windowName = string.Empty;
            return false;
        }

        windowBounds = bounds;
        windowName = GetWindowTitle(foregroundWindow);
        return true;
    }

    public static bool ActivateChromeWindow()
    {
        IntPtr chromeWindow = FindChromeWindow();

        if (chromeWindow == IntPtr.Zero)
        {
            AppLog.Info("No Chrome window found to activate.");
            return false;
        }

        ShowWindow(chromeWindow, SwRestore);
        bool activated = SetForegroundWindow(chromeWindow);
        AppLog.Info($"Chrome activation attempted. Activated={activated}; Title=\"{GetWindowTitle(chromeWindow)}\".");
        return activated;
    }

    public static bool TryGetChromeWindowHandle(out IntPtr chromeWindow)
    {
        chromeWindow = FindChromeWindow();
        return chromeWindow != IntPtr.Zero;
    }

    public static IReadOnlyList<IntPtr> GetChromeWindowHandles()
    {
        List<IntPtr> handles = [];

        EnumWindows((hwnd, _) =>
        {
            if (!IsWindowVisible(hwnd) ||
                !GetWindowClassName(hwnd).Equals("Chrome_WidgetWin_1", StringComparison.OrdinalIgnoreCase) ||
                !IsOwnedByChrome(hwnd) ||
                !GetWindowRect(hwnd, out NativeRectangle nativeRectangle))
            {
                return true;
            }

            Rectangle bounds = Rectangle.FromLTRB(
                nativeRectangle.Left,
                nativeRectangle.Top,
                nativeRectangle.Right,
                nativeRectangle.Bottom);

            if (bounds.Width >= 400 && bounds.Height >= 300)
            {
                handles.Add(hwnd);
            }

            return true;
        }, IntPtr.Zero);

        return handles;
    }

    private static IntPtr FindChromeWindow()
    {
        return GetChromeWindowHandles().FirstOrDefault();
    }

    private static string GetWindowClassName(IntPtr hwnd)
    {
        StringBuilder builder = new(256);
        GetClassName(hwnd, builder, builder.Capacity);
        return builder.ToString();
    }

    private static string GetWindowTitle(IntPtr hwnd)
    {
        int length = GetWindowTextLength(hwnd);

        if (length <= 0)
        {
            return string.Empty;
        }

        StringBuilder builder = new(length + 1);
        GetWindowText(hwnd, builder, builder.Capacity);
        return builder.ToString();
    }

    private static bool IsOwnedByChrome(IntPtr hwnd)
    {
        GetWindowThreadProcessId(hwnd, out uint processId);

        if (processId == 0)
        {
            return false;
        }

        try
        {
            using Process process = Process.GetProcessById((int)processId);
            return process.ProcessName.Equals("chrome", StringComparison.OrdinalIgnoreCase);
        }
        catch
        {
            return false;
        }
    }

    [DllImport("user32.dll")]
    private static extern bool IsWindowVisible(IntPtr hWnd);

    [DllImport("user32.dll")]
    private static extern bool EnumWindows(EnumWindowsProc lpEnumFunc, IntPtr lParam);

    [DllImport("user32.dll")]
    private static extern IntPtr GetForegroundWindow();

    [DllImport("user32.dll")]
    private static extern bool SetForegroundWindow(IntPtr hWnd);

    [DllImport("user32.dll")]
    private static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

    [DllImport("user32.dll", CharSet = CharSet.Unicode)]
    private static extern int GetClassName(IntPtr hWnd, StringBuilder lpClassName, int nMaxCount);

    [DllImport("user32.dll", CharSet = CharSet.Unicode)]
    private static extern int GetWindowText(IntPtr hWnd, StringBuilder lpString, int nMaxCount);

    [DllImport("user32.dll", CharSet = CharSet.Unicode)]
    private static extern int GetWindowTextLength(IntPtr hWnd);

    [DllImport("user32.dll")]
    private static extern bool GetWindowRect(IntPtr hWnd, out NativeRectangle lpRect);

    [DllImport("user32.dll")]
    private static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint lpdwProcessId);

    [StructLayout(LayoutKind.Sequential)]
    private struct NativeRectangle
    {
        public int Left;
        public int Top;
        public int Right;
        public int Bottom;
    }
}
