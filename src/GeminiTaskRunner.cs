using System.Diagnostics;

namespace GeminiForChromeManager;

internal sealed class GeminiTaskRunner
{
    private readonly ApprovalWatcher approvalWatcher;
    private readonly AppSettings settings;

    public GeminiTaskRunner(ApprovalWatcher approvalWatcher, AppSettings settings)
    {
        this.approvalWatcher = approvalWatcher;
        this.settings = settings;
    }

    public GeminiTaskRunResult Run(ScheduledGeminiTask task)
    {
        if (string.IsNullOrWhiteSpace(task.Prompt))
        {
            AppLog.Info($"Scheduled task \"{task.Name}\" skipped because prompt is empty.");
            task.LastResult = "Skipped: prompt is empty";
            return GeminiTaskRunResult.Exception(TaskRunExceptionCodes.PromptEmpty, "Skipped: prompt is empty");
        }

        AppLog.Info($"Scheduled task \"{task.Name}\" starting. ScheduleKind={task.ScheduleKind}; Reasoning={task.ReasoningLevel}; ChromeProfile={settings.ChromeProfileDirectory}; CompletionAction={task.CompletionAction}.");

        TimeSpan approvalWindow = TimeSpan.FromMinutes(15);
        approvalWatcher.AllowStartTaskApprovalsForScheduledTask(task.Name, approvalWindow);
        ChromeTaskSession chromeSession = ChromeTaskSession.CaptureBeforeRun();

        try
        {
            EnsureChromeIsOpen(settings.ChromeProfileDirectory);
            Thread.Sleep(1500);

            bool activated = ChromeWindowLocator.ActivateChromeWindow();
            if (!activated)
            {
                AppLog.Info($"Scheduled task \"{task.Name}\" could not confirm Chrome activation before opening Gemini side panel.");
            }

            Thread.Sleep(750);
            using ChromeGeminiSidePanelController sidePanel = new();
            bool sidePanelOpened = sidePanel.TryOpenSidePanel();

            if (!sidePanelOpened)
            {
                SendKeys.SendWait("%g");
                AppLog.Info($"Scheduled task \"{task.Name}\" sent Chrome Gemini side panel shortcut Alt+G as fallback.");
            }

            Thread.Sleep(2000);

            bool reasoningApplied = sidePanel.TryApplyReasoningLevel(task.ReasoningLevel);
            bool promptFocused = sidePanel.TryFocusPromptBox();

            if (!promptFocused)
            {
                task.LastResult = "Failed: Gemini prompt box not found";
                AppLog.Info($"Scheduled task \"{task.Name}\" stopped before paste because the Chrome Gemini side panel prompt box was not focused. SidePanelOpened={sidePanelOpened}; ReasoningApplied={reasoningApplied}.");
                sidePanel.TryCloseSidePanel();
                chromeSession.Cleanup();
                return GeminiTaskRunResult.Exception(TaskRunExceptionCodes.PromptBoxNotFound, "Failed: Gemini prompt box not found");
            }

            Clipboard.SetText(task.Prompt);
            SendKeys.SendWait("^v");
            Thread.Sleep(250);
            SendKeys.SendWait("{ENTER}");

            AppLog.Info($"Scheduled task \"{task.Name}\" prompt paste/send attempted in Chrome Gemini side panel. PromptLength={task.Prompt.Length}; Reasoning={task.ReasoningLevel}; SidePanelOpened={sidePanelOpened}; ReasoningApplied={reasoningApplied}; PromptFocused={promptFocused}.");
            GeminiTaskCompletionResult completion = sidePanel.WaitForTaskCompletion(
                TimeSpan.FromMinutes(Math.Clamp(settings.TaskCompletionTimeoutMinutes, 1, 240)),
                TimeSpan.FromSeconds(Math.Clamp(settings.TaskIdleStableSeconds, 5, 300)));

            task.LastResult = $"{completion.State}: {completion.Reason}";
            AppLog.Info($"Scheduled task \"{task.Name}\" completion monitor result. State={completion.State}; Reason=\"{completion.Reason}\".");
            sidePanel.TryCloseSidePanel();
            chromeSession.Cleanup();
            string exceptionCode = TaskRunExceptionCodes.FromCompletionState(completion.State);

            if (string.IsNullOrWhiteSpace(exceptionCode))
            {
                return GeminiTaskRunResult.Success(task.LastResult);
            }

            return GeminiTaskRunResult.Exception(exceptionCode, task.LastResult);
        }
        catch (Exception exception)
        {
            task.LastResult = "Failed: see diagnostic log";
            AppLog.Error($"Scheduled task \"{task.Name}\" failed while starting Chrome Gemini side panel prompt.", exception);
            chromeSession.Cleanup();
            return GeminiTaskRunResult.Exception(TaskRunExceptionCodes.UnexpectedRunnerError, "Failed: see diagnostic log");
        }
    }

    private static void EnsureChromeIsOpen(string chromeProfileDirectory)
    {
        string profileDirectory = ChromeProfileStore.NormalizeSelectedProfile(chromeProfileDirectory);

        Process.Start(new ProcessStartInfo
        {
            FileName = "chrome.exe",
            Arguments = $"--profile-directory=\"{profileDirectory.Replace("\"", "\\\"")}\" about:blank",
            UseShellExecute = true
        });

        AppLog.Info($"Chrome launch requested for profile directory \"{profileDirectory}\".");
    }
}
