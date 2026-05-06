using System.Text.Json;
using System.Text.Json.Serialization;

namespace GeminiForChromeManager;

internal sealed class AppSettings
{
    public bool StartWithWindows { get; set; }

    public ThemeMode Theme { get; set; } = AppSettingsDefaults.Theme;

    public double PollSeconds { get; set; } = AppSettingsDefaults.PollSeconds;

    public bool DiagnosticLoggingEnabled { get; set; } = AppSettingsDefaults.DiagnosticLoggingEnabled;

    public int MaxLogKilobytes { get; set; } = AppSettingsDefaults.MaxLogKilobytes;

    public int MaxLogFiles { get; set; } = AppSettingsDefaults.MaxLogFiles;

    public int HeartbeatMinutes { get; set; } = AppSettingsDefaults.HeartbeatMinutes;
}

internal static class AppSettingsDefaults
{
    public const ThemeMode Theme = ThemeMode.Auto;
    public const bool DiagnosticLoggingEnabled = true;
    public const double PollSeconds = 1.0;
    public const int MaxLogKilobytes = 256;
    public const int MaxLogFiles = 5;
    public const int HeartbeatMinutes = 1;
}

internal static class SettingsStore
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true
    };

    static SettingsStore()
    {
        JsonOptions.Converters.Add(new JsonStringEnumConverter());
    }

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
