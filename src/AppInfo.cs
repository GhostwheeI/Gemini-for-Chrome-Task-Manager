namespace GeminiForChromeManager;

internal static class AppInfo
{
    public const string AppName = "Gemini for Chrome Task Manager";
    public const string Publisher = "Ghostwheel";
    public const string Version = "1.0.0";

    public static string ExecutablePath =>
        Environment.ProcessPath ?? Application.ExecutablePath;
}
