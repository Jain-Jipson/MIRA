using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using System;

public class Visitor
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string Id { get; set; } = string.Empty; // Fix nullable warning

    [BsonElement("name")]
    public string Name { get; set; } = string.Empty; // Fix nullable warning

    [BsonElement("face_encoding")]
    public string FaceEncoding { get; set; } = string.Empty; // Fix nullable warning

    [BsonElement("first_seen")]
    public DateTime FirstSeen { get; set; } = DateTime.UtcNow;

    [BsonElement("image_path")]
    public string ImagePath { get; set; } = string.Empty; // ✅ Add this property
}
