namespace GeminiForChromeManager;

internal sealed class TaskEditorForm : Form
{
    private readonly TextBox nameTextBox = new();
    private readonly TextBox promptTextBox = new();
    private readonly ComboBox scheduleKindComboBox = new();
    private readonly DateTimePicker nextRunDatePicker = new();
    private readonly DateTimePicker nextRunTimePicker = new();
    private readonly NumericUpDown repeatEveryInput = new();
    private readonly ComboBox repeatUnitComboBox = new();
    private readonly ComboBox completionActionComboBox = new();
    private readonly CheckBox runImmediatelyCheckBox = new();
    private readonly CheckBox enabledCheckBox = new();
    private readonly Label repeatLabel = new();
    private readonly ScheduledGeminiTask task;

    public TaskEditorForm(ScheduledGeminiTask task)
    {
        this.task = task.Clone();
        this.task.NormalizeLegacyValues();

        Text = "Gemini Scheduled Task";
        StartPosition = FormStartPosition.CenterScreen;
        MinimumSize = new Size(680, 560);
        Size = new Size(760, 680);

        BuildLayout();
        LoadTask();
        ApplyScheduleState();
    }

    public ScheduledGeminiTask ResultTask => task;

    public bool RunImmediately => runImmediatelyCheckBox.Checked;

    private void BuildLayout()
    {
        TableLayoutPanel root = new()
        {
            Dock = DockStyle.Fill,
            Padding = new Padding(12),
            ColumnCount = 2,
            RowCount = 10
        };
        root.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 160));
        root.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));

        AddLabel(root, "Task name", 0);
        nameTextBox.Dock = DockStyle.Fill;
        root.Controls.Add(nameTextBox, 1, 0);

        AddLabel(root, "Prompt", 1);
        promptTextBox.Multiline = true;
        promptTextBox.ScrollBars = ScrollBars.Vertical;
        promptTextBox.Dock = DockStyle.Fill;
        root.Controls.Add(promptTextBox, 1, 1);

        AddLabel(root, "Schedule", 2);
        scheduleKindComboBox.DropDownStyle = ComboBoxStyle.DropDownList;
        scheduleKindComboBox.Items.AddRange(["Run once", "Repeat every"]);
        scheduleKindComboBox.Dock = DockStyle.Left;
        scheduleKindComboBox.Width = 180;
        scheduleKindComboBox.SelectedIndexChanged += (_, _) => ApplyScheduleState();
        root.Controls.Add(scheduleKindComboBox, 1, 2);

        AddLabel(root, "Next run date", 3);
        nextRunDatePicker.Format = DateTimePickerFormat.Short;
        nextRunDatePicker.Dock = DockStyle.Left;
        root.Controls.Add(nextRunDatePicker, 1, 3);

        AddLabel(root, "Next run time", 4);
        nextRunTimePicker.Format = DateTimePickerFormat.Time;
        nextRunTimePicker.ShowUpDown = true;
        nextRunTimePicker.Dock = DockStyle.Left;
        root.Controls.Add(nextRunTimePicker, 1, 4);

        repeatLabel.Text = "Run every";
        repeatLabel.AutoSize = true;
        repeatLabel.Anchor = AnchorStyles.Left;
        root.Controls.Add(repeatLabel, 0, 5);

        FlowLayoutPanel repeatPanel = new()
        {
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.LeftToRight
        };
        repeatEveryInput.Minimum = 1;
        repeatEveryInput.Maximum = 365;
        repeatEveryInput.Width = 80;
        repeatUnitComboBox.DropDownStyle = ComboBoxStyle.DropDownList;
        repeatUnitComboBox.Items.AddRange(["Minutes", "Hours", "Days"]);
        repeatUnitComboBox.Width = 120;
        repeatPanel.Controls.Add(repeatEveryInput);
        repeatPanel.Controls.Add(repeatUnitComboBox);
        root.Controls.Add(repeatPanel, 1, 5);

        AddLabel(root, "When run", 6);
        completionActionComboBox.DropDownStyle = ComboBoxStyle.DropDownList;
        completionActionComboBox.Items.AddRange(["Do Nothing", "Show notification"]);
        completionActionComboBox.Dock = DockStyle.Left;
        completionActionComboBox.Width = 200;
        root.Controls.Add(completionActionComboBox, 1, 6);

        AddLabel(root, "Run Immediately", 7);
        runImmediatelyCheckBox.Text = "Run this task as soon as it is saved";
        runImmediatelyCheckBox.Dock = DockStyle.Fill;
        root.Controls.Add(runImmediatelyCheckBox, 1, 7);

        AddLabel(root, "Enabled", 8);
        enabledCheckBox.Text = "Enable this task";
        enabledCheckBox.Checked = true;
        enabledCheckBox.Dock = DockStyle.Fill;
        root.Controls.Add(enabledCheckBox, 1, 8);

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
        saveButton.Click += (_, _) => SaveTask();

        Button cancelButton = new()
        {
            Text = "Cancel",
            DialogResult = DialogResult.Cancel,
            Width = 90
        };

        buttons.Controls.Add(saveButton);
        buttons.Controls.Add(cancelButton);
        root.Controls.Add(buttons, 1, 9);

        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 34));
        root.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

        for (int row = 2; row <= 9; row++)
        {
            root.RowStyles.Add(new RowStyle(SizeType.Absolute, row == 9 ? 44 : 36));
        }

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

    private void LoadTask()
    {
        nameTextBox.Text = task.Name;
        promptTextBox.Text = task.Prompt;
        scheduleKindComboBox.SelectedIndex = task.ScheduleKind == ScheduleKind.Once ? 0 : 1;
        nextRunDatePicker.Value = task.NextRunLocal.Date;
        nextRunTimePicker.Value = DateTime.Today.Add(task.NextRunLocal.TimeOfDay);
        repeatEveryInput.Value = Math.Clamp(task.RepeatEvery, 1, 365);
        repeatUnitComboBox.SelectedIndex = task.RepeatUnit switch
        {
            RepeatUnit.Hours => 1,
            RepeatUnit.Days => 2,
            _ => 0
        };
        completionActionComboBox.SelectedIndex = task.CompletionAction == CompletionAction.ShowNotification ? 1 : 0;
        runImmediatelyCheckBox.Checked = false;
        enabledCheckBox.Checked = task.Enabled;
    }

    private void ApplyScheduleState()
    {
        bool repeats = scheduleKindComboBox.SelectedIndex == 1;
        repeatLabel.Enabled = repeats;
        repeatEveryInput.Enabled = repeats;
        repeatUnitComboBox.Enabled = repeats;
    }

    private void SaveTask()
    {
        if (string.IsNullOrWhiteSpace(nameTextBox.Text))
        {
            MessageBox.Show("Enter a task name.", Text, MessageBoxButtons.OK, MessageBoxIcon.Warning);
            DialogResult = DialogResult.None;
            return;
        }

        if (string.IsNullOrWhiteSpace(promptTextBox.Text))
        {
            MessageBox.Show("Enter the Gemini prompt for this task.", Text, MessageBoxButtons.OK, MessageBoxIcon.Warning);
            DialogResult = DialogResult.None;
            return;
        }

        task.Name = nameTextBox.Text.Trim();
        task.Prompt = promptTextBox.Text;
        task.ScheduleKind = scheduleKindComboBox.SelectedIndex == 0 ? ScheduleKind.Once : ScheduleKind.RepeatEvery;
        task.NextRunLocal = nextRunDatePicker.Value.Date.Add(nextRunTimePicker.Value.TimeOfDay);
        task.RepeatEvery = (int)repeatEveryInput.Value;
        task.RepeatUnit = repeatUnitComboBox.SelectedIndex switch
        {
            1 => RepeatUnit.Hours,
            2 => RepeatUnit.Days,
            _ => RepeatUnit.Minutes
        };
        task.CompletionAction = completionActionComboBox.SelectedIndex == 1
            ? CompletionAction.ShowNotification
            : CompletionAction.DoNothing;
        task.Enabled = enabledCheckBox.Checked;
    }
}
