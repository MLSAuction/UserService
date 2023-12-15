using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using System;


namespace UserService.Models
{
    public class UserDTO
    {
        [BsonId]
        [BsonRepresentation(BsonType.String)] // This attribute specifies that the Guid should be stored as a string
        public Guid? UserId { get; set; }
        public string? Username { get; set;}
        public string? Password { get; set;}
        public string? FirstName { get; set;}
        public string? Surname { get; set;}
        public int? Phone { get; set;}
        public string? Email { get; set;}
        public string? Address { get; set;}
        public DateTime RegistrationDate { get; set;}
    }
}