using System.Text.Json;

namespace GeminiForChromeManager;

internal sealed class AppSettings
{
    public bool StartWithWindows { get; set; }

    public double PollSeconds { get; set; } = 1.0;

    public bool DiagnosticLoggingEnabled { get; set; } = true;

    public int MaxLogKilobytes { get; set; } = 256;

    public int MaxLogFiles { get; set; } = 5;

    public int HeartbeatMinutes { get; set; } = 1;
}

internal static class SettingsStore
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true
    };

    public static string SettingsDirectory =>
        Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "Gemini for Chrome Task Manager");

    private static string LegacySettingsDirectory =>
        Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "Gemini for Chrome Manager");

    public static string SettingsPath => Path.Combine(SettingsDirectory, "settings.json");

    public static AppSettings Load()
    {
        try
        {
            MigrateLegacyDirectoryIfNeeded();

            if (!File.Exists(SettingsPath))
            {
                return new AppSettings();
            }

            string json = File.ReadAllText(SettingsPath);
            return JsonSerializer.Deserialize<AppSettings>(json) ?? new AppSettings();
        }
        catch
        {
            return new AppSettings();
        }
    }

    public static void Save(AppSettings settings)
    {
        MigrateLegacyDirectoryIfNeeded();
        Directory.CreateDirectory(SettingsDirectory);
        string json = JsonSerializer.Serialize(settings, JsonOptions);
        File.WriteAllText(SettingsPath, json);
    }

    private static void MigrateLegacyDirectoryIfNeeded()
    {
        if (!Directory.Exists(LegacySettingsDirectory) || Directory.Exists(SettingsDirectory))
        {
            return;
        }

        Directory.CreateDirectory(Path.GetDirectoryName(SettingsDirectory)!);
        Directory.Move(LegacySettingsDirectory, SettingsDirectory);
    }
}
