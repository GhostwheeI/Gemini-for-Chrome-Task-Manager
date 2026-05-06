namespace GeminiForChromeManager;

internal sealed class TrayApplicationContext : ApplicationContext
{
    private readonly AppSettings settings;
    private readonly ApprovalWatcher watcher;
    private readonly SchedulerService scheduler;
    private readonly NotifyIcon notifyIcon;
    private readonly ToolStripMenuItem statusItem;
    private readonly ContextMenuStrip menu;

    public TrayApplicationContext()
    {
        settings = SettingsStore.Load();
        AppLog.Configure(settings);
        AppLog.Info($"{AppInfo.AppName} {AppInfo.Version} starting. Executable={AppInfo.ExecutablePath}");
        settings.StartWithWindows = ApprovalWatcher.IsStartWithWindowsEnabled();
        SettingsStore.Save(settings);

        watcher = new ApprovalWatcher(settings);
        scheduler = new SchedulerService(new GeminiTaskRunner(watcher));
        scheduler.StatusChanged += (_, status) => UpdateStatus(status);
        scheduler.NotificationRequested += (_, message) => ShowNotification(message);

        statusItem = new ToolStripMenuItem("Status: Idle")
        {
            Enabled = false
        };

        menu = BuildMenu();
        menu.Opening += (_, _) => RebuildMenu();

        notifyIcon = new NotifyIcon
        {
            Icon = AppIcons.CreateTrayIcon(enabled: true),
            Text = AppInfo.AppName,
            Visible = true,
            ContextMenuStrip = menu
        };
        notifyIcon.DoubleClick += (_, _) => ShowSummary();

        watcher.Start();
        scheduler.Start();
    }

    private ContextMenuStrip BuildMenu()
    {
        ContextMenuStrip menu = new();
        return menu;
    }

    private void RebuildMenu()
    {
        menu.Items.Clear();
        ToolStripMenuItem titleItem = new(AppInfo.AppName)
        {
            Enabled = false
        };

        ToolStripMenuItem createTaskItem = new("Create a Task for Gemini in Chrome");
        createTaskItem.Click += (_, _) => CreateTask();

        ToolStripMenuItem enableDisableMenu = new("Enable/Disable Tasks");
        PopulateEnableDisableMenu(enableDisableMenu);

        ToolStripMenuItem configureMenu = new("Configure Tasks");
        PopulateConfigureMenu(configureMenu);

        ToolStripMenuItem settingsItem = new("Settings");
        settingsItem.Click += (_, _) => ShowSettings();

        ToolStripMenuItem aboutItem = new("About");
        aboutItem.Click += (_, _) => ShowSummary();

        ToolStripMenuItem exitItem = new("Exit");
        exitItem.Click += (_, _) => ExitThread();

        menu.Items.Add(titleItem);
        menu.Items.Add(statusItem);
        menu.Items.Add(new ToolStripSeparator());
        menu.Items.Add(createTaskItem);
        menu.Items.Add(enableDisableMenu);
        menu.Items.Add(configureMenu);
        menu.Items.Add(new ToolStripSeparator());
        menu.Items.Add(settingsItem);
        menu.Items.Add(aboutItem);
        menu.Items.Add(new ToolStripSeparator());
        menu.Items.Add(exitItem);
    }

    private void PopulateEnableDisableMenu(ToolStripMenuItem parent)
    {
        if (scheduler.Tasks.Count == 0)
        {
            parent.DropDownItems.Add(new ToolStripMenuItem("None") { Enabled = false });
            return;
        }

        foreach (ScheduledGeminiTask task in scheduler.Tasks.OrderBy(task => task.Name))
        {
            ToolStripMenuItem item = new(task.Name)
            {
                Checked = task.Enabled,
                CheckOnClick = true
            };
            item.Click += (_, _) =>
            {
                task.Enabled = item.Checked;
                scheduler.ReplaceTask(task);
                AppLog.Info($"Task \"{task.Name}\" enabled set to {task.Enabled} from tray menu.");
            };
            parent.DropDownItems.Add(item);
        }
    }

    private void PopulateConfigureMenu(ToolStripMenuItem parent)
    {
        if (scheduler.Tasks.Count == 0)
        {
            parent.DropDownItems.Add(new ToolStripMenuItem("None") { Enabled = false });
            return;
        }

        foreach (ScheduledGeminiTask task in scheduler.Tasks.OrderBy(task => task.Name))
        {
            ToolStripMenuItem item = new(task.Name);
            item.Click += (_, _) => EditTask(task);
            parent.DropDownItems.Add(item);
        }
    }

    private void UpdateStatus(string status)
    {
        statusItem.Text = $"Status: {status}";
        notifyIcon.Text = BuildNotifyText(status);
    }

    private static string BuildNotifyText(string status)
    {
        string value = $"{AppInfo.AppName}\n{status}";
        return value.Length <= 63 ? value : value[..63];
    }

    private void ShowSummary()
    {
        string message =
            $"{AppInfo.AppName} {AppInfo.Version}\n\n" +
            $"Scheduled tasks: {scheduler.Tasks.Count}\n" +
            $"Enabled tasks: {scheduler.Tasks.Count(task => task.Enabled)}\n" +
            $"Start with Windows: {YesNo(settings.StartWithWindows)}\n\n" +
            $"Current status: {scheduler.StatusText}";

        MessageBox.Show(message, AppInfo.AppName, MessageBoxButtons.OK, MessageBoxIcon.Information);
    }

    private static string YesNo(bool value) => value ? "Yes" : "No";

    private void CreateTask()
    {
        ScheduledGeminiTask task = new();

        using TaskEditorForm editor = new(task);

        if (editor.ShowDialog() == DialogResult.OK)
        {
            ScheduledGeminiTask savedTask = editor.ResultTask;
            scheduler.AddTask(savedTask);

            if (editor.RunImmediately)
            {
                scheduler.RunNow(savedTask);
            }
        }
    }

    private void EditTask(ScheduledGeminiTask task)
    {
        using TaskEditorForm editor = new(task);

        if (editor.ShowDialog() == DialogResult.OK)
        {
            ScheduledGeminiTask savedTask = editor.ResultTask;
            scheduler.ReplaceTask(savedTask);

            if (editor.RunImmediately)
            {
                scheduler.RunNow(savedTask);
            }
        }
    }

    private void ShowSettings()
    {
        using SettingsForm form = new(settings);

        if (form.ShowDialog() != DialogResult.OK)
        {
            return;
        }

        ApprovalWatcher.SetStartWithWindows(settings.StartWithWindows);
        SettingsStore.Save(settings);
        AppLog.Configure(settings);
        watcher.ApplyInterval();
        AppLog.Info($"Settings changed. StartWithWindows={settings.StartWithWindows}; Diagnostics={settings.DiagnosticLoggingEnabled}; MaxLogKB={settings.MaxLogKilobytes}; MaxLogFiles={settings.MaxLogFiles}; HeartbeatMinutes={settings.HeartbeatMinutes}; PollSeconds={settings.PollSeconds}.");
    }

    private void ShowNotification(string message)
    {
        notifyIcon.ShowBalloonTip(
            6000,
            AppInfo.AppName,
            message,
            ToolTipIcon.Info);
    }

    protected override void ExitThreadCore()
    {
        notifyIcon.Visible = false;
        notifyIcon.Icon?.Dispose();
        notifyIcon.Dispose();
        watcher.Dispose();
        scheduler.Dispose();
        SettingsStore.Save(settings);
        AppLog.Info($"{AppInfo.AppName} exiting.");
        base.ExitThreadCore();
    }
}
