namespace GeminiForChromeManager;

internal static class AppInfo
{
    public const string AppName = "Gemini for Chrome Task Manager";
    public const string Publisher = "Ghostwheel";
    public const string Version = "0.11.1";

    public static string ExecutablePath =>
        Environment.ProcessPath ?? Application.ExecutablePath;
}
