namespace GeminiForChromeManager;

internal static class AppInfo
{
    public const string AppName = "Gemini for Chrome Task Manager";
    public const string Publisher = "Ghostwheel";
    public const string Version = "0.9.0";

    public static string ExecutablePath =>
        Environment.ProcessPath ?? Application.ExecutablePath;
}
