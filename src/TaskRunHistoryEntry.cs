using System.Text.Json.Serialization;

namespace GeminiForChromeManager;

internal sealed class TaskRunHistoryEntry
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N");

    public string TaskId { get; set; } = string.Empty;

    public string TaskName { get; set; } = string.Empty;

    public DateTime StartedLocal { get; set; }

    public DateTime EndedLocal { get; set; }

    public bool HadException { get; set; }

    public string ExceptionCode { get; set; } = TaskRunExceptionCodes.None;

    public string Result { get; set; } = string.Empty;

    [JsonIgnore]
    public string ExceptionDescription =>
        HadException ? TaskRunExceptionCodes.Describe(ExceptionCode) : "No exception";
}
