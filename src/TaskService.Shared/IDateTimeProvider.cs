namespace TaskService.Shared;

public interface IDateTimeProvider
{
    DateTime UtcNow { get; }
}
