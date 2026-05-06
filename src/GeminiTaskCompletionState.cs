namespace GeminiForChromeManager;

internal enum GeminiTaskCompletionState
{
    Completed,
    Error,
    Interrupted,
    TimedOut
}

internal sealed record GeminiTaskCompletionResult(
    GeminiTaskCompletionState State,
    string Reason);
