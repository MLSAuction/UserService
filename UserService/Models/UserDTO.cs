using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace UserService.Models
{
    public class UserDTO
    {
        [BsonId]
        public int? UserId { get; set; }
        public string? Username { get; set;}
        public string? Password { get; set;}
        public string? FirstName { get; set;}
        public string? LastName { get; set;}
        public int? Phone { get; set;}
        public string? Email { get; set;}
        public string? Address { get; set;}
    }


}
