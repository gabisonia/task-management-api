namespace TaskService.Shared;

public sealed class ProblemDetailsResponse
{
    public int Status { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Detail { get; set; }
    public string? Instance { get; set; }
    public string? Type { get; set; }
    public string? ErrorCode { get; set; }
    public string? CorrelationId { get; set; }
    public Dictionary<string, object>? Extensions { get; set; }
}

