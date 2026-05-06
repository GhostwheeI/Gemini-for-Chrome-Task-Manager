namespace GeminiForChromeManager;

internal sealed class TaskHistoryForm : Form
{
    private readonly ScheduledGeminiTask task;
    private readonly DataGridView grid = new();

    public TaskHistoryForm(ScheduledGeminiTask task)
    {
        this.task = task;
        Text = $"Task History - {task.Name}";
        StartPosition = FormStartPosition.CenterScreen;
        MinimumSize = new Size(820, 380);
        Size = new Size(940, 480);

        BuildLayout();
        RefreshGrid();
        AppTheme.Apply(this);
    }

    private void BuildLayout()
    {
        grid.Dock = DockStyle.Fill;
        grid.AutoGenerateColumns = false;
        grid.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
        grid.MultiSelect = false;
        grid.ReadOnly = true;
        grid.AllowUserToAddRows = false;
        grid.AllowUserToDeleteRows = false;
        grid.RowHeadersVisible = false;

        grid.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = nameof(TaskRunHistoryEntry.StartedLocal), HeaderText = "Started", Width = 160 });
        grid.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = nameof(TaskRunHistoryEntry.EndedLocal), HeaderText = "Ended", Width = 160 });
        grid.Columns.Add(new DataGridViewCheckBoxColumn { DataPropertyName = nameof(TaskRunHistoryEntry.HadException), HeaderText = "Exception", Width = 80 });
        grid.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = nameof(TaskRunHistoryEntry.ExceptionCode), HeaderText = "Code", Width = 90 });
        grid.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = nameof(TaskRunHistoryEntry.Result), HeaderText = "Result", AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill });

        TextBox codeReference = new()
        {
            Dock = DockStyle.Bottom,
            Height = 86,
            Multiline = true,
            ReadOnly = true,
            ScrollBars = ScrollBars.Vertical,
            Text =
                $"{TaskRunExceptionCodes.PromptEmpty}: {TaskRunExceptionCodes.Describe(TaskRunExceptionCodes.PromptEmpty)}{Environment.NewLine}" +
                $"{TaskRunExceptionCodes.PromptBoxNotFound}: {TaskRunExceptionCodes.Describe(TaskRunExceptionCodes.PromptBoxNotFound)}{Environment.NewLine}" +
                $"{TaskRunExceptionCodes.GeminiError}: {TaskRunExceptionCodes.Describe(TaskRunExceptionCodes.GeminiError)}{Environment.NewLine}" +
                $"{TaskRunExceptionCodes.GeminiInterrupted}: {TaskRunExceptionCodes.Describe(TaskRunExceptionCodes.GeminiInterrupted)}{Environment.NewLine}" +
                $"{TaskRunExceptionCodes.GeminiTimedOut}: {TaskRunExceptionCodes.Describe(TaskRunExceptionCodes.GeminiTimedOut)}{Environment.NewLine}" +
                $"{TaskRunExceptionCodes.UnexpectedRunnerError}: {TaskRunExceptionCodes.Describe(TaskRunExceptionCodes.UnexpectedRunnerError)}"
        };

        FlowLayoutPanel buttons = new()
        {
            Dock = DockStyle.Bottom,
            Height = 48,
            FlowDirection = FlowDirection.LeftToRight,
            Padding = new Padding(8)
        };

        buttons.Controls.Add(MakeButton("Refresh", (_, _) => RefreshGrid()));
        buttons.Controls.Add(MakeButton("Close", (_, _) => Close()));

        Controls.Add(grid);
        Controls.Add(codeReference);
        Controls.Add(buttons);
    }

    private static Button MakeButton(string text, EventHandler click)
    {
        Button button = new()
        {
            Text = text,
            Width = 92,
            Height = 30
        };
        button.Click += click;
        return button;
    }

    private void RefreshGrid()
    {
        grid.DataSource = null;
        grid.DataSource = TaskRunHistoryStore.LoadForTask(task.Id).ToList();
    }
}
