using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

public class Faq
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string? Id { get; set; }  // Allow null for MongoDB auto-generation

    [BsonElement("question")]
    public string Question { get; set; } = string.Empty; // Default to empty string

    [BsonElement("answer")]
    public string Answer { get; set; } = string.Empty; // Default to empty string

    [BsonElement("category")]
    public string Category { get; set; } = string.Empty; // Default to empty string
}
