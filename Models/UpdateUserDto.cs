using MongoDB.Bson.Serialization.Attributes;

namespace Veearve.Models
{
    public class UpdateUserDto
    {
        public string? Name { get; set; }

        public string? ApartmentNumber { get; set; }
        public string? Email { get; set; }
    }
}
