namespace TaskService.Application.Dtos.Projects;

public sealed class ProjectAnalyticsResponse
{
    public string ProjectId { get; set; } = string.Empty;
    public string ProjectName { get; set; } = string.Empty;
    public int TotalTasks { get; set; }
    public Dictionary<string, int> TasksByStatus { get; set; } = new();
    public Dictionary<string, int> TasksByPriority { get; set; } = new();
    public double CompletionPercentage { get; set; }
    public int OverdueTasks { get; set; }
    public double? AverageCompletionDays { get; set; }
    public DateRange? DateRange { get; set; }
}

public sealed class DateRange
{
    public DateTime Start { get; set; }
    public DateTime End { get; set; }
}

