namespace GeminiForChromeManager;

internal sealed class SettingsForm : Form
{
    private readonly AppSettings settings;
    private readonly CheckBox startWithWindowsCheckBox = new();
    private readonly CheckBox diagnosticsCheckBox = new();
    private readonly NumericUpDown maxLogKilobytesInput = new();
    private readonly NumericUpDown maxLogFilesInput = new();
    private readonly NumericUpDown heartbeatMinutesInput = new();
    private readonly NumericUpDown pollSecondsInput = new();

    public SettingsForm(AppSettings settings)
    {
        this.settings = settings;
        Text = "Settings";
        StartPosition = FormStartPosition.CenterScreen;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        ClientSize = new Size(470, 315);

        BuildLayout();
        LoadSettings();
    }

    private void BuildLayout()
    {
        TableLayoutPanel root = new()
        {
            Dock = DockStyle.Fill,
            Padding = new Padding(12),
            ColumnCount = 2,
            RowCount = 8
        };
        root.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 190));
        root.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));

        AddLabel(root, "Start with Windows", 0);
        startWithWindowsCheckBox.Text = "Launch when you sign in";
        root.Controls.Add(startWithWindowsCheckBox, 1, 0);

        AddLabel(root, "Diagnostic logging", 1);
        diagnosticsCheckBox.Text = "Enabled";
        root.Controls.Add(diagnosticsCheckBox, 1, 1);

        AddLabel(root, "Max log size (KB)", 2);
        maxLogKilobytesInput.Minimum = 32;
        maxLogKilobytesInput.Maximum = 4096;
        root.Controls.Add(maxLogKilobytesInput, 1, 2);

        AddLabel(root, "Max log files", 3);
        maxLogFilesInput.Minimum = 1;
        maxLogFilesInput.Maximum = 20;
        root.Controls.Add(maxLogFilesInput, 1, 3);

        AddLabel(root, "Heartbeat every (minutes)", 4);
        heartbeatMinutesInput.Minimum = 1;
        heartbeatMinutesInput.Maximum = 60;
        root.Controls.Add(heartbeatMinutesInput, 1, 4);

        AddLabel(root, "Scan interval (seconds)", 5);
        pollSecondsInput.DecimalPlaces = 1;
        pollSecondsInput.Increment = 0.5M;
        pollSecondsInput.Minimum = 0.5M;
        pollSecondsInput.Maximum = 30;
        root.Controls.Add(pollSecondsInput, 1, 5);

        Button openLogsButton = new()
        {
            Text = "Open Logs",
            Width = 100
        };
        openLogsButton.Click += (_, _) => AppLog.OpenLogFolder();
        root.Controls.Add(openLogsButton, 1, 6);

        FlowLayoutPanel buttons = new()
        {
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.RightToLeft
        };

        Button saveButton = new()
        {
            Text = "Save",
            DialogResult = DialogResult.OK,
            Width = 90
        };
        saveButton.Click += (_, _) => SaveSettings();

        Button cancelButton = new()
        {
            Text = "Cancel",
            DialogResult = DialogResult.Cancel,
            Width = 90
        };

        buttons.Controls.Add(saveButton);
        buttons.Controls.Add(cancelButton);
        root.Controls.Add(buttons, 1, 7);

        for (int row = 0; row < 7; row++)
        {
            root.RowStyles.Add(new RowStyle(SizeType.Absolute, 36));
        }

        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 44));
        Controls.Add(root);
        AcceptButton = saveButton;
        CancelButton = cancelButton;
    }

    private static void AddLabel(TableLayoutPanel root, string text, int row)
    {
        Label label = new()
        {
            Text = text,
            AutoSize = true,
            Anchor = AnchorStyles.Left,
            TextAlign = ContentAlignment.MiddleLeft
        };
        root.Controls.Add(label, 0, row);
    }

    private void LoadSettings()
    {
        startWithWindowsCheckBox.Checked = settings.StartWithWindows;
        diagnosticsCheckBox.Checked = settings.DiagnosticLoggingEnabled;
        maxLogKilobytesInput.Value = Math.Clamp(settings.MaxLogKilobytes, 32, 4096);
        maxLogFilesInput.Value = Math.Clamp(settings.MaxLogFiles, 1, 20);
        heartbeatMinutesInput.Value = Math.Clamp(settings.HeartbeatMinutes, 1, 60);
        pollSecondsInput.Value = (decimal)Math.Clamp(settings.PollSeconds, 0.5, 30);
    }

    private void SaveSettings()
    {
        settings.StartWithWindows = startWithWindowsCheckBox.Checked;
        settings.DiagnosticLoggingEnabled = diagnosticsCheckBox.Checked;
        settings.MaxLogKilobytes = (int)maxLogKilobytesInput.Value;
        settings.MaxLogFiles = (int)maxLogFilesInput.Value;
        settings.HeartbeatMinutes = (int)heartbeatMinutesInput.Value;
        settings.PollSeconds = (double)pollSecondsInput.Value;
    }
}
