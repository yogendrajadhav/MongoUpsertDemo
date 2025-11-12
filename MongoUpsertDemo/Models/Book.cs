using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace MongoUpsertDemo.Models;

public class Book
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string  Id { get; set; } = string.Empty;

    [BsonElement("title")]
    public string Title { get; set; } = string.Empty;

    [BsonElement("author")]     
    public string Author { get; set; } = string.Empty;

    [BsonElement("price")]
    public decimal Price { get; set; } = 0;

    [BsonElement("isDeleted")]
    [BsonDefaultValue(false)]
    public bool IsDeleted { get; set; } = false;
}