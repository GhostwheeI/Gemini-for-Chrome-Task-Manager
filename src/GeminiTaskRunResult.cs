namespace GeminiForChromeManager;

internal sealed record GeminiTaskRunResult(
    bool Completed,
    string Result,
    string ExceptionCode = TaskRunExceptionCodes.None)
{
    public bool HasException => !string.IsNullOrWhiteSpace(ExceptionCode);

    public static GeminiTaskRunResult Success(string result)
    {
        return new GeminiTaskRunResult(true, result);
    }

    public static GeminiTaskRunResult Exception(string code, string result)
    {
        return new GeminiTaskRunResult(false, result, code);
    }
}
