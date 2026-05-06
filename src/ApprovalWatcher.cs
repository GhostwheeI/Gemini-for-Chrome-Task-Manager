using System.Diagnostics;
using Microsoft.Win32;

namespace GeminiForChromeManager;

internal sealed class ApprovalWatcher : IDisposable
{
    private readonly AppSettings settings;
    private readonly System.Windows.Forms.Timer timer;
    private DateTime lastClickUtc = DateTime.MinValue;
    private DateTime lastHeartbeatUtc = DateTime.MinValue;
    private DateTime lastRejectedCandidateLogUtc = DateTime.MinValue;
    private DateTime scheduledApprovalUntilUtc = DateTime.MinValue;
    private string scheduledApprovalTaskName = string.Empty;
    private Point lastClickPoint = Point.Empty;
    private long totalScans;
    private long activeScans;
    private long disabledApprovalScans;
    private long noPromptScans;
    private long candidateScans;
    private long cooldownSkips;
    private long rejectedCandidates;
    private long clickCount;
    private double totalScanMilliseconds;
    private double lastScanMilliseconds;

    public event EventHandler<string>? StatusChanged;

    public ApprovalWatcher(AppSettings settings)
    {
        this.settings = settings;
        timer = new System.Windows.Forms.Timer();
        timer.Tick += (_, _) => ScanAndClickIfNeeded();
        ApplyInterval();
    }

    public string StatusText { get; private set; } = "Idle";

    public void Start()
    {
        ApplyInterval();
        timer.Start();
        SetStatus("Idle");
        AppLog.Info($"Watcher started. PollSeconds={settings.PollSeconds}; StartTaskApprovals=ScheduledTasksOnly");
    }

    public void ApplyInterval()
    {
        int intervalMilliseconds = Math.Max(500, (int)(settings.PollSeconds * 1000));
        timer.Interval = intervalMilliseconds;
    }

    public void ScanAndClickIfNeeded()
    {
        Stopwatch stopwatch = Stopwatch.StartNew();

        try
        {
            ScanAndClickIfNeededCore();
        }
        catch (Exception exception)
        {
            SetStatus("Error; see diagnostic log");
            AppLog.Error("Scan failed.", exception);
        }
        finally
        {
            stopwatch.Stop();
            RecordScanDuration(stopwatch.Elapsed.TotalMilliseconds);
            WriteHeartbeatIfDue();
        }
    }

    private void ScanAndClickIfNeededCore()
    {
        totalScans++;

        if (!IsStartTaskApprovalAllowed())
        {
            disabledApprovalScans++;
            SetStatus("Idle");
            return;
        }

        activeScans++;
        ScreenButtonCandidate? candidate = ScreenButtonFinder.FindBestLightBlueActionButton();

        if (candidate is null)
        {
            noPromptScans++;
            SetStatus("Watching; no Start Task prompt visible");
            return;
        }

        candidateScans++;
        Point clickPoint = new(candidate.Value.CenterX, candidate.Value.CenterY);

        if (!ChromeWindowLocator.IsPointInsideForegroundChromeWindow(clickPoint, out Rectangle chromeBounds, out string chromeWindowName))
        {
            rejectedCandidates++;
            SetStatus("Candidate rejected outside active Chrome window");
            LogRejectedCandidateIfDue(clickPoint, candidate.Value, "OutsideForegroundChromeWindow", null, null);
            return;
        }

        if (!IsInLikelyGeminiPanel(clickPoint, chromeBounds))
        {
            rejectedCandidates++;
            SetStatus("Candidate rejected outside Gemini panel area");
            LogRejectedCandidateIfDue(clickPoint, candidate.Value, "OutsideRightPanelArea", chromeWindowName, chromeBounds);
            return;
        }

        AppLog.Info($"Start Task candidate accepted. Center={clickPoint.X},{clickPoint.Y}; Size={candidate.Value.Width}x{candidate.Value.Height}; Area={candidate.Value.Area}; ChromeWindow=\"{chromeWindowName}\"; ChromeBounds={FormatRectangle(chromeBounds)}.");

        if (IsCooldownActive(clickPoint))
        {
            cooldownSkips++;
            SetStatus($"Cooldown at {clickPoint.X},{clickPoint.Y}");
            AppLog.Info($"Click skipped by cooldown at {clickPoint.X},{clickPoint.Y}.");
            return;
        }

        NativeMouse.Click(clickPoint.X, clickPoint.Y);
        lastClickUtc = DateTime.UtcNow;
        lastClickPoint = clickPoint;
        clickCount++;
        SetStatus($"Clicked Start Task at {clickPoint.X},{clickPoint.Y}");
        AppLog.Info($"Clicked Start Task approval at {clickPoint.X},{clickPoint.Y}.");
    }

    private void RecordScanDuration(double elapsedMilliseconds)
    {
        lastScanMilliseconds = elapsedMilliseconds;
        totalScanMilliseconds += elapsedMilliseconds;
    }

    private void WriteHeartbeatIfDue()
    {
        DateTime nowUtc = DateTime.UtcNow;

        int heartbeatSeconds = Math.Max(1, settings.HeartbeatMinutes) * 60;

        if ((nowUtc - lastHeartbeatUtc).TotalSeconds < heartbeatSeconds)
        {
            return;
        }

        lastHeartbeatUtc = nowUtc;
        double averageScanMilliseconds = totalScans == 0 ? 0 : totalScanMilliseconds / totalScans;

        AppLog.Info(
            "Heartbeat. " +
            $"Status=\"{StatusText}\"; " +
            $"TotalScans={totalScans}; ActiveScans={activeScans}; " +
        $"DisabledApprovalScans={disabledApprovalScans}; NoPromptScans={noPromptScans}; " +
            $"CandidateScans={candidateScans}; RejectedCandidates={rejectedCandidates}; CooldownSkips={cooldownSkips}; Clicks={clickCount}; " +
            $"LastScanMs={lastScanMilliseconds:F1}; AvgScanMs={averageScanMilliseconds:F1}; " +
            $"PollSeconds={settings.PollSeconds}; ScheduledApprovalActive={IsScheduledApprovalActive()}.");
    }

    public void AllowStartTaskApprovalsForScheduledTask(string taskName, TimeSpan duration)
    {
        scheduledApprovalTaskName = taskName;
        scheduledApprovalUntilUtc = DateTime.UtcNow.Add(duration);
        AppLog.Info($"Start Task approvals forced on for scheduled task \"{taskName}\" until {scheduledApprovalUntilUtc:O}.");
    }

    private bool IsStartTaskApprovalAllowed()
    {
        return IsScheduledApprovalActive();
    }

    private bool IsScheduledApprovalActive()
    {
        return DateTime.UtcNow <= scheduledApprovalUntilUtc;
    }

    private static bool IsInLikelyGeminiPanel(Point point, Rectangle chromeBounds)
    {
        int minimumPanelLeft = chromeBounds.Left + (int)(chromeBounds.Width * 0.60);
        int maximumPanelLeft = chromeBounds.Right - 900;
        int panelLeft = Math.Max(minimumPanelLeft, maximumPanelLeft);
        int contentTop = chromeBounds.Top + 120;
        int contentBottom = chromeBounds.Bottom - 80;

        return point.X >= panelLeft &&
               point.X <= chromeBounds.Right - 20 &&
               point.Y >= contentTop &&
               point.Y <= contentBottom;
    }

    private static string FormatRectangle(Rectangle rectangle)
    {
        return $"{rectangle.Left},{rectangle.Top},{rectangle.Width}x{rectangle.Height}";
    }

    private void LogRejectedCandidateIfDue(
        Point clickPoint,
        ScreenButtonCandidate candidate,
        string reason,
        string? chromeWindowName,
        Rectangle? chromeBounds)
    {
        DateTime nowUtc = DateTime.UtcNow;

        if ((nowUtc - lastRejectedCandidateLogUtc).TotalSeconds < 30)
        {
            return;
        }

        lastRejectedCandidateLogUtc = nowUtc;

        string chromeText = chromeBounds is null
            ? string.Empty
            : $"; ChromeWindow=\"{chromeWindowName}\"; ChromeBounds={FormatRectangle(chromeBounds.Value)}";

        AppLog.Info($"Candidate rejected. Center={clickPoint.X},{clickPoint.Y}; Size={candidate.Width}x{candidate.Height}; Area={candidate.Area}; Reason={reason}{chromeText}.");
    }

    public static bool IsStartWithWindowsEnabled()
    {
        using RegistryKey? key = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Run", false);
        return key?.GetValue(AppInfo.AppName) is string value &&
               value.Contains(AppInfo.ExecutablePath, StringComparison.OrdinalIgnoreCase);
    }

    public static void SetStartWithWindows(bool enabled)
    {
        using RegistryKey key = Registry.CurrentUser.CreateSubKey(@"Software\Microsoft\Windows\CurrentVersion\Run");

        if (enabled)
        {
            key.SetValue(AppInfo.AppName, $"\"{AppInfo.ExecutablePath}\"");
        }
        else
        {
            key.DeleteValue(AppInfo.AppName, false);
        }

        AppLog.Info($"Start with Windows set to {enabled}.");
    }

    public void OpenSettingsFolder()
    {
        Directory.CreateDirectory(SettingsStore.SettingsDirectory);
        Process.Start(new ProcessStartInfo
        {
            FileName = SettingsStore.SettingsDirectory,
            UseShellExecute = true
        });
    }

    private bool IsCooldownActive(Point candidate)
    {
        TimeSpan sinceLastClick = DateTime.UtcNow - lastClickUtc;
        int distanceX = Math.Abs(candidate.X - lastClickPoint.X);
        int distanceY = Math.Abs(candidate.Y - lastClickPoint.Y);

        return sinceLastClick.TotalSeconds < 6 && distanceX < 20 && distanceY < 20;
    }

    private void SetStatus(string value)
    {
        if (StatusText == value)
        {
            return;
        }

        StatusText = value;
        StatusChanged?.Invoke(this, value);
    }

    public void Dispose()
    {
        timer.Stop();
        timer.Dispose();
        AppLog.Info("Watcher stopped.");
    }
}
