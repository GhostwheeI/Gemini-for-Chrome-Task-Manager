namespace GeminiForChromeManager;

internal sealed class SchedulerService : IDisposable
{
    private readonly List<ScheduledGeminiTask> tasks;
    private readonly GeminiTaskRunner runner;
    private readonly System.Windows.Forms.Timer timer;
    private bool isRunningDueTasks;

    public event EventHandler<string>? StatusChanged;

    public event EventHandler<string>? NotificationRequested;

    public SchedulerService(GeminiTaskRunner runner)
    {
        this.runner = runner;
        tasks = ScheduledTaskStore.Load();
        timer = new System.Windows.Forms.Timer
        {
            Interval = 15_000
        };
        timer.Tick += (_, _) => RunDueWork();
    }

    public IReadOnlyList<ScheduledGeminiTask> Tasks => tasks;

    public string StatusText { get; private set; } = "No Scheduled Tasks";

    public void Start()
    {
        timer.Start();
        SetStatus(GetIdleStatus());
        AppLog.Info($"Scheduler started. TaskCount={tasks.Count}.");
        RunDueWork();
    }

    public void SaveTasks()
    {
        ScheduledTaskStore.Save(tasks);
        SetStatus(GetIdleStatus());
        AppLog.Info($"Scheduled tasks saved. TaskCount={tasks.Count}.");
    }

    public void AddTask(ScheduledGeminiTask task)
    {
        tasks.Add(task);
        SaveTasks();
    }

    public void ReplaceTask(ScheduledGeminiTask task)
    {
        int index = tasks.FindIndex(existing => existing.Id == task.Id);

        if (index >= 0)
        {
            tasks[index] = task;
        }
        else
        {
            tasks.Add(task);
        }

        SaveTasks();
    }

    public void DeleteTask(string id)
    {
        tasks.RemoveAll(task => task.Id == id);
        SaveTasks();
    }

    public void RunNow(ScheduledGeminiTask task)
    {
        RunTask(task, DateTime.Now, manualRun: true);
        SaveTasks();
    }

    private void RunDueWork()
    {
        if (isRunningDueTasks)
        {
            return;
        }

        isRunningDueTasks = true;

        try
        {
            DateTime nowLocal = DateTime.Now;

            foreach (ScheduledGeminiTask task in tasks.Where(task => task.Enabled && task.NextRunLocal <= nowLocal).ToList())
            {
                RunTask(task, nowLocal, manualRun: false);
            }

            ScheduledTaskStore.Save(tasks);
        }
        finally
        {
            isRunningDueTasks = false;
        }
    }

    private void RunTask(ScheduledGeminiTask task, DateTime nowLocal, bool manualRun)
    {
        SetStatus($"Running scheduled task: {task.Name}");
        AppLog.Info($"Scheduler triggering task \"{task.Name}\". ManualRun={manualRun}; NextRunLocal={task.NextRunLocal:g}.");

        DateTime startedLocal = DateTime.Now;
        GeminiTaskRunResult result = runner.Run(task);
        DateTime endedLocal = DateTime.Now;
        task.AdvanceAfterRun(nowLocal);
        task.LastResult = result.Result;
        TaskRunHistoryStore.Append(new TaskRunHistoryEntry
        {
            TaskId = task.Id,
            TaskName = task.Name,
            StartedLocal = startedLocal,
            EndedLocal = endedLocal,
            HadException = result.HasException,
            ExceptionCode = result.ExceptionCode,
            Result = result.Result
        });
        AppLog.Info($"Task history recorded for \"{task.Name}\". Started={startedLocal:g}; Ended={endedLocal:g}; HadException={result.HasException}; ExceptionCode={result.ExceptionCode}.");

        if (!result.Completed)
        {
            SetStatus(GetIdleStatus());
            return;
        }

        string message = $"Scheduled Gemini task finished: {task.Name}";

        if (task.CompletionAction == CompletionAction.ShowNotification)
        {
            NotificationRequested?.Invoke(this, message);
        }

        SetStatus(GetIdleStatus());
    }

    private string GetIdleStatus()
    {
        return tasks.Count == 0 ? "No Scheduled Tasks" : "Idle";
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
        AppLog.Info("Scheduler stopped.");
    }
}
