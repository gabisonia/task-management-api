using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using TaskService.Domain.Common;

namespace TaskService.Domain.ProjectManagement;

public class Project : IHasAudit
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public ObjectId Id { get; set; }
    [BsonElement("ownerId")]
    [BsonRepresentation(BsonType.String)]
    public string OwnerId { get; set; } = string.Empty;
    [BsonElement("name")]
    public string Name { get; set; } = string.Empty;
    [BsonElement("description")]
    public string? Description { get; set; }

    [BsonElement("status")]
    [BsonRepresentation(BsonType.String)]
    public ProjectStatus Status { get; set; }

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
