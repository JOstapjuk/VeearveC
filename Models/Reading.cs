using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace Veearve.Models
{
    public class Reading
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        [JsonPropertyName("_id")]
        public string Id { get; set; }

        [BsonElement("apartmentNumber")]
        public string ApartmentNumber { get; set; }

        [BsonElement("userName")]
        public string UserName { get; set; }

        [BsonElement("userId")]
        [BsonRepresentation(BsonType.ObjectId)]
        public string UserId { get; set; }

        [BsonElement("date")]
        public DateTime Date { get; set; }

        [BsonElement("coldWater")]
        public double ColdWater { get; set; }

        [BsonElement("hotWater")]
        public double HotWater { get; set; }

        [BsonElement("isPaid")]
        public bool IsPaid { get; set; }

        [BsonElement("amount")]
        public double Amount { get; set; }

        [BsonElement("createdAt")]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        [BsonElement("__v")]
        public int __v { get; set; } = 0;
    }
}
