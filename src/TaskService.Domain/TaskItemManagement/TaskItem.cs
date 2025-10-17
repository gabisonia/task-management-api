using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using TaskService.Domain.Common;

namespace TaskService.Domain.TaskItemManagement;

public class TaskItem : IHasAudit
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public ObjectId Id { get; set; }
    [BsonElement("projectId")]
    [BsonRepresentation(BsonType.ObjectId)]
    public ObjectId ProjectId { get; set; }
    [BsonElement("title")]
    public string Title { get; set; } = string.Empty;
    [BsonElement("description")]
    public string? Description { get; set; }
    [BsonElement("status")]
    [BsonRepresentation(BsonType.String)]
    public TaskItemStatus Status { get; set; }
    [BsonElement("priority")]
    [BsonRepresentation(BsonType.String)]
    public Priority Priority { get; set; }
    [BsonElement("assigneeUserId")]
    [BsonRepresentation(BsonType.String)]
    public string? AssigneeUserId { get; set; }
    [BsonElement("dueDate")]
    [BsonDateTimeOptions(Kind = DateTimeKind.Utc)]
    public DateTime? DueDate { get; set; }
    [BsonElement("tags")]
    public string[] Tags { get; set; } = [];
    [BsonElement("createdAt")]
    [BsonDateTimeOptions(Kind = DateTimeKind.Utc)]
    public DateTime CreatedAt { get; set; }
    [BsonElement("updatedAt")]
    [BsonDateTimeOptions(Kind = DateTimeKind.Utc)]
    public DateTime UpdatedAt { get; set; }
    [BsonElement("createdBy")]
    public string CreatedBy { get; set; } = string.Empty;
    [BsonElement("updatedBy")]
    public string UpdatedBy { get; set; } = string.Empty;
    [BsonElement("isDeleted")]
    public bool IsDeleted { get; set; }
}
