using System.Diagnostics;

namespace GeminiForChromeManager;

internal sealed class GeminiTaskRunner
{
    private const string GeminiUrl = "https://gemini.google.com/app";
    private readonly ApprovalWatcher approvalWatcher;

    public GeminiTaskRunner(ApprovalWatcher approvalWatcher)
    {
        this.approvalWatcher = approvalWatcher;
    }

    public bool Run(ScheduledGeminiTask task)
    {
        if (string.IsNullOrWhiteSpace(task.Prompt))
        {
            AppLog.Info($"Scheduled task \"{task.Name}\" skipped because prompt is empty.");
            task.LastResult = "Skipped: prompt is empty";
            return false;
        }

        AppLog.Info($"Scheduled task \"{task.Name}\" starting. ScheduleKind={task.ScheduleKind}; CompletionAction={task.CompletionAction}.");

        TimeSpan approvalWindow = TimeSpan.FromMinutes(15);
        approvalWatcher.AllowStartTaskApprovalsForScheduledTask(task.Name, approvalWindow);

        try
        {
            OpenGeminiInChrome();
            Thread.Sleep(3000);
            ChromeWindowLocator.ActivateChromeWindow();
            Thread.Sleep(750);

            Clipboard.SetText(task.Prompt);
            SendKeys.SendWait("^v");
            Thread.Sleep(250);
            SendKeys.SendWait("{ENTER}");

            AppLog.Info($"Scheduled task \"{task.Name}\" prompt paste/send attempted. PromptLength={task.Prompt.Length}.");
            task.LastResult = "Prompt sent to Chrome/Gemini";
            return true;
        }
        catch (Exception exception)
        {
            task.LastResult = "Failed: see diagnostic log";
            AppLog.Error($"Scheduled task \"{task.Name}\" failed while starting Gemini prompt.", exception);
            return false;
        }
    }

    private static void OpenGeminiInChrome()
    {
        Process.Start(new ProcessStartInfo
        {
            FileName = "chrome.exe",
            Arguments = GeminiUrl,
            UseShellExecute = true
        });
    }
}
