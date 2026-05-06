using System.Text.Json;

namespace GeminiForChromeManager;

internal static class ScheduledTaskStore
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true
    };

    public static string TasksPath => Path.Combine(SettingsStore.SettingsDirectory, "tasks.json");

    public static List<ScheduledGeminiTask> Load()
    {
        try
        {
            if (!File.Exists(TasksPath))
            {
                return [];
            }

            string json = File.ReadAllText(TasksPath);
            List<ScheduledGeminiTask> tasks = JsonSerializer.Deserialize<List<ScheduledGeminiTask>>(json) ?? [];

            foreach (ScheduledGeminiTask task in tasks)
            {
                task.NormalizeLegacyValues();
            }

            return tasks;
        }
        catch (Exception exception)
        {
            AppLog.Error("Failed to load scheduled task store.", exception);
            return [];
        }
    }

    public static void Save(IReadOnlyCollection<ScheduledGeminiTask> tasks)
    {
        Directory.CreateDirectory(SettingsStore.SettingsDirectory);
        string json = JsonSerializer.Serialize(tasks, JsonOptions);
        File.WriteAllText(TasksPath, json);
    }
}
