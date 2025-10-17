namespace TaskService.Domain.Common;

public interface IHasAudit
{
    DateTime CreatedAt { get; set; }
    DateTime UpdatedAt { get; set; }
    string CreatedBy { get; set; }
    string UpdatedBy { get; set; }
    bool IsDeleted { get; set; }
}
