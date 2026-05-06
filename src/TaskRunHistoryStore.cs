using System.Text.Json;

namespace GeminiForChromeManager;

internal static class TaskRunHistoryStore
{
    private const int MaxEntriesPerTask = 100;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true
    };

    public static string HistoryPath => Path.Combine(SettingsStore.SettingsDirectory, "task-history.json");

    public static List<TaskRunHistoryEntry> Load()
    {
        try
        {
            if (!File.Exists(HistoryPath))
            {
                return [];
            }

            string json = File.ReadAllText(HistoryPath);
            return JsonSerializer.Deserialize<List<TaskRunHistoryEntry>>(json) ?? [];
        }
        catch (Exception exception)
        {
            AppLog.Error("Failed to load task run history store.", exception);
            return [];
        }
    }

    public static IReadOnlyList<TaskRunHistoryEntry> LoadForTask(string taskId)
    {
        return Load()
            .Where(entry => entry.TaskId == taskId)
            .OrderByDescending(entry => entry.StartedLocal)
            .ToList();
    }

    public static void Append(TaskRunHistoryEntry entry)
    {
        try
        {
            List<TaskRunHistoryEntry> entries = Load();
            entries.Add(entry);

            List<TaskRunHistoryEntry> pruned = entries
                .GroupBy(existing => existing.TaskId)
                .SelectMany(group => group
                    .OrderByDescending(existing => existing.StartedLocal)
                    .Take(MaxEntriesPerTask))
                .OrderBy(existing => existing.StartedLocal)
                .ToList();

            Directory.CreateDirectory(SettingsStore.SettingsDirectory);
            string json = JsonSerializer.Serialize(pruned, JsonOptions);
            File.WriteAllText(HistoryPath, json);
        }
        catch (Exception exception)
        {
            AppLog.Error("Failed to append task run history entry.", exception);
        }
    }
}
