namespace GeminiForChromeManager;

internal enum ScheduleKind
{
    Once,
    RepeatEvery
}

internal enum CompletionAction
{
    DoNothing,
    ShowNotification
}

internal enum RepeatUnit
{
    Minutes,
    Hours,
    Days
}

internal sealed class ScheduledGeminiTask
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N");

    public string Name { get; set; } = "New Gemini task";

    public bool Enabled { get; set; } = true;

    public string Prompt { get; set; } = string.Empty;

    public ScheduleKind ScheduleKind { get; set; } = ScheduleKind.Once;

    public DateTime NextRunLocal { get; set; } = DateTime.Now.AddMinutes(5);

    public int RepeatEvery { get; set; } = 1;

    public RepeatUnit RepeatUnit { get; set; } = RepeatUnit.Hours;

    public int RepeatEveryMinutes { get; set; } = 60;

    public int RunWindowMinutes { get; set; } = 30;

    public CompletionAction CompletionAction { get; set; } = CompletionAction.ShowNotification;

    public DateTime? LastRunLocal { get; set; }

    public string LastResult { get; set; } = "Never run";

    public ScheduledGeminiTask Clone()
    {
        return new ScheduledGeminiTask
        {
            Id = Id,
            Name = Name,
            Enabled = Enabled,
            Prompt = Prompt,
            ScheduleKind = ScheduleKind,
            NextRunLocal = NextRunLocal,
            RepeatEvery = RepeatEvery,
            RepeatUnit = RepeatUnit,
            RepeatEveryMinutes = RepeatEveryMinutes,
            RunWindowMinutes = RunWindowMinutes,
            CompletionAction = CompletionAction,
            LastRunLocal = LastRunLocal,
            LastResult = LastResult
        };
    }

    public void AdvanceAfterRun(DateTime nowLocal)
    {
        LastRunLocal = nowLocal;

        if (ScheduleKind == ScheduleKind.Once)
        {
            Enabled = false;
            LastResult = "Completed one-time run";
            return;
        }

        TimeSpan interval = GetRepeatInterval();
        DateTime next = NextRunLocal;

        while (next <= nowLocal)
        {
            next = next.Add(interval);
        }

        NextRunLocal = next;
        LastResult = $"Next run at {NextRunLocal:g}";
    }

    public TimeSpan GetRepeatInterval()
    {
        int value = Math.Max(1, RepeatEvery);

        return RepeatUnit switch
        {
            RepeatUnit.Hours => TimeSpan.FromHours(value),
            RepeatUnit.Days => TimeSpan.FromDays(value),
            _ => TimeSpan.FromMinutes(value)
        };
    }

    public string RepeatDescription =>
        ScheduleKind == ScheduleKind.Once
            ? string.Empty
            : $"{Math.Max(1, RepeatEvery)} {RepeatUnit.ToString().ToLowerInvariant()}";

    public void NormalizeLegacyValues()
    {
        if (RepeatEvery > 0)
        {
            return;
        }

        int legacyMinutes = Math.Max(1, RepeatEveryMinutes);

        if (legacyMinutes % 1440 == 0)
        {
            RepeatEvery = legacyMinutes / 1440;
            RepeatUnit = RepeatUnit.Days;
        }
        else if (legacyMinutes % 60 == 0)
        {
            RepeatEvery = legacyMinutes / 60;
            RepeatUnit = RepeatUnit.Hours;
        }
        else
        {
            RepeatEvery = legacyMinutes;
            RepeatUnit = RepeatUnit.Minutes;
        }
    }
}
