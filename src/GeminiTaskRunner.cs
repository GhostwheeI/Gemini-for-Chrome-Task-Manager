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

    public bool Run(ScheduledGeminiTask task)
    {
        if (string.IsNullOrWhiteSpace(task.Prompt))
        {
            AppLog.Info($"Scheduled task \"{task.Name}\" skipped because prompt is empty.");
            task.LastResult = "Skipped: prompt is empty";
            return false;
        }

        AppLog.Info($"Scheduled task \"{task.Name}\" starting. ScheduleKind={task.ScheduleKind}; Reasoning={task.ReasoningLevel}; ChromeProfile={settings.ChromeProfileDirectory}; CompletionAction={task.CompletionAction}.");

        TimeSpan approvalWindow = TimeSpan.FromMinutes(15);
        approvalWatcher.AllowStartTaskApprovalsForScheduledTask(task.Name, approvalWindow);

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
                AppLog.Info($"Scheduled task \"{task.Name}\" is falling back to current Chrome focus for prompt paste.");
            }

            Clipboard.SetText(task.Prompt);
            SendKeys.SendWait("^v");
            Thread.Sleep(250);
            SendKeys.SendWait("{ENTER}");

            AppLog.Info($"Scheduled task \"{task.Name}\" prompt paste/send attempted in Chrome Gemini side panel. PromptLength={task.Prompt.Length}; Reasoning={task.ReasoningLevel}; SidePanelOpened={sidePanelOpened}; ReasoningApplied={reasoningApplied}; PromptFocused={promptFocused}.");
            task.LastResult = "Prompt sent to Chrome Gemini side panel";
            return true;
        }
        catch (Exception exception)
        {
            task.LastResult = "Failed: see diagnostic log";
            AppLog.Error($"Scheduled task \"{task.Name}\" failed while starting Chrome Gemini side panel prompt.", exception);
            return false;
        }
    }

    private static void EnsureChromeIsOpen(string chromeProfileDirectory)
    {
        string profileDirectory = ChromeProfileStore.NormalizeSelectedProfile(chromeProfileDirectory);

        Process.Start(new ProcessStartInfo
        {
            FileName = "chrome.exe",
            Arguments = $"--profile-directory=\"{profileDirectory.Replace("\"", "\\\"")}\"",
            UseShellExecute = true
        });

        AppLog.Info($"Chrome launch requested for profile directory \"{profileDirectory}\".");
    }
}
