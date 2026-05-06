using System.Runtime.InteropServices;

namespace GeminiForChromeManager;

internal static class NativeMouse
{
    private const uint MouseEventLeftDown = 0x0002;
    private const uint MouseEventLeftUp = 0x0004;

    [DllImport("user32.dll")]
    private static extern bool SetCursorPos(int x, int y);

    [DllImport("user32.dll")]
    private static extern void mouse_event(uint dwFlags, uint dx, uint dy, uint dwData, UIntPtr dwExtraInfo);

    public static void Click(int x, int y)
    {
        SetCursorPos(x, y);
        Thread.Sleep(75);
        mouse_event(MouseEventLeftDown, 0, 0, 0, UIntPtr.Zero);
        Thread.Sleep(75);
        mouse_event(MouseEventLeftUp, 0, 0, 0, UIntPtr.Zero);
    }
}
