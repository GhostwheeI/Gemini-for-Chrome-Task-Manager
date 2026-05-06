namespace GeminiForChromeManager;

internal static class AppLog
{
    private static readonly object Sync = new();
    private static bool enabled = true;
    private static long maxLogBytes = 256 * 1024;
    private static int maxLogFiles = 5;

    public static string LogDirectory => Path.Combine(SettingsStore.SettingsDirectory, "logs");

    private static string CurrentLogPath => Path.Combine(LogDirectory, "diagnostic.log");

    public static void Configure(AppSettings settings)
    {
        lock (Sync)
        {
            enabled = settings.DiagnosticLoggingEnabled;
            maxLogBytes = Math.Max(32, settings.MaxLogKilobytes) * 1024L;
            maxLogFiles = Math.Clamp(settings.MaxLogFiles, 1, 20);
        }
    }

    public static void Info(string message)
    {
        Write("INFO", message, null);
    }

    public static void Error(string message, Exception exception)
    {
        Write("ERROR", message, exception);
    }

    public static void OpenLogFolder()
    {
        Directory.CreateDirectory(LogDirectory);
        System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
        {
            FileName = LogDirectory,
            UseShellExecute = true
        });
    }

    private static void Write(string level, string message, Exception? exception)
    {
        lock (Sync)
        {
            try
            {
                if (!enabled)
                {
                    return;
                }

                Directory.CreateDirectory(LogDirectory);
                RotateIfNeeded();
                PruneOldLogs();

                string line = $"{DateTimeOffset.Now:yyyy-MM-dd HH:mm:ss.fff zzz} [{level}] {message}";
                File.AppendAllText(CurrentLogPath, line + Environment.NewLine);

                if (exception is not null)
                {
                    File.AppendAllText(CurrentLogPath, exception + Environment.NewLine);
                }
            }
            catch
            {
                // Logging must never break the tray app.
            }
        }
    }

    private static void RotateIfNeeded()
    {
        FileInfo file = new(CurrentLogPath);

        if (!file.Exists || file.Length < maxLogBytes)
        {
            return;
        }

        string stamp = DateTime.Now.ToString("yyyyMMdd-HHmmss");
        string archivePath = Path.Combine(LogDirectory, $"diagnostic-{stamp}.log");
        File.Move(CurrentLogPath, archivePath, true);
    }

    private static void PruneOldLogs()
    {
        DirectoryInfo directory = new(LogDirectory);

        if (!directory.Exists)
        {
            return;
        }

        FileInfo[] logs = directory
            .GetFiles("diagnostic*.log")
            .OrderByDescending(file => file.LastWriteTimeUtc)
            .ToArray();

        for (int index = maxLogFiles; index < logs.Length; index++)
        {
            TryDelete(logs[index]);
        }
    }

    private static void TryDelete(FileInfo file)
    {
        try
        {
            file.Delete();
        }
        catch
        {
            // Best-effort cleanup only.
        }
    }
}
