using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace Veearve.Models
{
    public class User
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        [JsonPropertyName("_id")]
        public string Id { get; set; }

        [BsonElement("email")]
        [Required]
        [EmailAddress]
        public string Email { get; set; }

        [BsonElement("password")]
        [Required]
        public string Password { get; set; }

        [BsonElement("name")]
        [Required]
        public string Name { get; set; }

        [BsonElement("apartmentNumber")]
        public string? ApartmentNumber { get; set; }

        [BsonElement("role")]
        [Required]
        public string Role { get; set; } = "user";

        [BsonElement("createdAt")]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        [BsonElement("__v")]
        public int __v { get; set; } = 0;
    }
}
