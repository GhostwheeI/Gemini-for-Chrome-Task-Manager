using System.Diagnostics;

namespace GeminiForChromeManager;

internal sealed class ChromeTaskSession
{
    private readonly HashSet<int> existingProcessIds;
    private readonly bool hadChromeBeforeRun;

    private ChromeTaskSession(HashSet<int> existingProcessIds)
    {
        this.existingProcessIds = existingProcessIds;
        hadChromeBeforeRun = existingProcessIds.Count > 0;
    }

    public static ChromeTaskSession CaptureBeforeRun()
    {
        HashSet<int> processIds = Process
            .GetProcessesByName("chrome")
            .Select(process => process.Id)
            .ToHashSet();

        AppLog.Info($"Captured Chrome pre-run state. ExistingChromeProcesses={processIds.Count}.");
        return new ChromeTaskSession(processIds);
    }

    public async Task CleanupAsync()
    {
        if (!hadChromeBeforeRun)
        {
            CloseChromeProcessesStartedForTask();
            return;
        }

        await CloseActiveTaskTabAsync();
    }

    private void CloseChromeProcessesStartedForTask()
    {
        Process[] currentProcesses = Process.GetProcessesByName("chrome");
        int closedCount = 0;

        foreach (Process process in currentProcesses)
        {
            if (existingProcessIds.Contains(process.Id))
            {
                continue;
            }

            try
            {
                if (process.MainWindowHandle != IntPtr.Zero)
                {
                    process.CloseMainWindow();
                    closedCount++;
                }
            }
            catch (Exception exception)
            {
                AppLog.Error($"Failed while closing Chrome process {process.Id} after scheduled task.", exception);
            }
        }

        AppLog.Info($"Chrome cleanup closed task-created Chrome windows. Count={closedCount}.");
    }

    private static async Task CloseActiveTaskTabAsync()
    {
        if (!ChromeWindowLocator.ActivateChromeWindow())
        {
            AppLog.Info("Chrome cleanup skipped tab close because Chrome could not be activated.");
            return;
        }

        await Task.Delay(250);
        SendKeys.SendWait("^w");
        AppLog.Info("Chrome cleanup requested active task tab close with Ctrl+W.");
    }
}
