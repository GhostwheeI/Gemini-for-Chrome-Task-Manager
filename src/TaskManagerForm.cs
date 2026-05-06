namespace GeminiForChromeManager;

internal sealed class TaskManagerForm : Form
{
    private readonly SchedulerService scheduler;
    private readonly DataGridView grid = new();

    public TaskManagerForm(SchedulerService scheduler)
    {
        this.scheduler = scheduler;
        Text = "Gemini for Chrome Scheduled Tasks";
        StartPosition = FormStartPosition.CenterScreen;
        MinimumSize = new Size(860, 480);
        Size = new Size(980, 560);

        BuildLayout();
        RefreshGrid();
    }

    private ScheduledGeminiTask? SelectedTask =>
        grid.CurrentRow?.DataBoundItem as ScheduledGeminiTask;

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
        grid.DoubleClick += (_, _) => EditSelectedTask();

        grid.Columns.Add(new DataGridViewCheckBoxColumn { DataPropertyName = nameof(ScheduledGeminiTask.Enabled), HeaderText = "Enabled", Width = 70 });
        grid.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = nameof(ScheduledGeminiTask.Name), HeaderText = "Name", Width = 220 });
        grid.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = nameof(ScheduledGeminiTask.ScheduleDescription), HeaderText = "Schedule", Width = 110 });
        grid.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = nameof(ScheduledGeminiTask.NextRunLocal), HeaderText = "Next run", Width = 150 });
        grid.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = nameof(ScheduledGeminiTask.RepeatDescription), HeaderText = "Repeat", Width = 110 });
        grid.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = nameof(ScheduledGeminiTask.ReasoningDescription), HeaderText = "Reasoning", Width = 90 });
        grid.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = nameof(ScheduledGeminiTask.CompletionDescription), HeaderText = "When run", Width = 140 });
        grid.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = nameof(ScheduledGeminiTask.LastResult), HeaderText = "Last result", AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill });

        FlowLayoutPanel buttons = new()
        {
            Dock = DockStyle.Bottom,
            Height = 48,
            FlowDirection = FlowDirection.LeftToRight,
            Padding = new Padding(8)
        };

        buttons.Controls.Add(MakeButton("Add", (_, _) => AddTask()));
        buttons.Controls.Add(MakeButton("Edit", (_, _) => EditSelectedTask()));
        buttons.Controls.Add(MakeButton("Delete", (_, _) => DeleteSelectedTask()));
        buttons.Controls.Add(MakeButton("Run now", (_, _) => RunSelectedTaskNow()));
        buttons.Controls.Add(MakeButton("Refresh", (_, _) => RefreshGrid()));
        buttons.Controls.Add(MakeButton("Close", (_, _) => Close()));

        Controls.Add(grid);
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
        grid.DataSource = scheduler.Tasks
            .OrderBy(task => !task.Enabled)
            .ThenBy(task => task.NextRunLocal)
            .ToList();
    }

    private void AddTask()
    {
        ScheduledGeminiTask task = new();

        using TaskEditorForm editor = new(task);

        if (editor.ShowDialog(this) == DialogResult.OK)
        {
            ScheduledGeminiTask savedTask = editor.ResultTask;
            scheduler.AddTask(savedTask);

            if (editor.RunImmediately)
            {
                scheduler.RunNow(savedTask);
            }

            RefreshGrid();
        }
    }

    private void EditSelectedTask()
    {
        ScheduledGeminiTask? selected = SelectedTask;

        if (selected is null)
        {
            return;
        }

        using TaskEditorForm editor = new(selected);

        if (editor.ShowDialog(this) == DialogResult.OK)
        {
            ScheduledGeminiTask savedTask = editor.ResultTask;
            scheduler.ReplaceTask(savedTask);

            if (editor.RunImmediately)
            {
                scheduler.RunNow(savedTask);
            }

            RefreshGrid();
        }
    }

    private void DeleteSelectedTask()
    {
        ScheduledGeminiTask? selected = SelectedTask;

        if (selected is null)
        {
            return;
        }

        DialogResult result = MessageBox.Show(
            $"Delete \"{selected.Name}\"?",
            Text,
            MessageBoxButtons.YesNo,
            MessageBoxIcon.Question);

        if (result == DialogResult.Yes)
        {
            scheduler.DeleteTask(selected.Id);
            RefreshGrid();
        }
    }

    private void RunSelectedTaskNow()
    {
        ScheduledGeminiTask? selected = SelectedTask;

        if (selected is null)
        {
            return;
        }

        scheduler.RunNow(selected);
        RefreshGrid();
    }
}
