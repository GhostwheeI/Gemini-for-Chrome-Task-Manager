namespace GeminiForChromeManager;

internal static class TaskRunExceptionCodes
{
    public const string None = "";
    public const string PromptEmpty = "GCTM-001";
    public const string PromptBoxNotFound = "GCTM-002";
    public const string GeminiError = "GCTM-003";
    public const string GeminiInterrupted = "GCTM-004";
    public const string GeminiTimedOut = "GCTM-005";
    public const string UnexpectedRunnerError = "GCTM-006";

    public static string Describe(string? code)
    {
        return code switch
        {
            PromptEmpty => "Task prompt was empty, so nothing could be sent to Gemini.",
            PromptBoxNotFound => "Gemini side panel opened, but the prompt box could not be found or focused.",
            GeminiError => "Gemini displayed a definite error or retry state.",
            GeminiInterrupted => "Gemini displayed an interrupted, stopped, or cancelled state.",
            GeminiTimedOut => "Gemini did not return to idle before the configured completion timeout.",
            UnexpectedRunnerError => "The task runner hit an unexpected application exception.",
            _ => "No exception was detected."
        };
    }

    public static string FromCompletionState(GeminiTaskCompletionState state)
    {
        return state switch
        {
            GeminiTaskCompletionState.Error => GeminiError,
            GeminiTaskCompletionState.Interrupted => GeminiInterrupted,
            GeminiTaskCompletionState.TimedOut => GeminiTimedOut,
            _ => None
        };
    }
}
