using Microsoft.AspNetCore.Mvc.Formatters;
using MongoDB.Driver;
using UserService.Models;
using UserService.Repositories.DBContext;


namespace UserService.Repositories
{
    public class UserRepository : IUserRepository
    {
        private readonly ILogger<UserRepository> _logger;
        private readonly IConfiguration _configuration;
        private readonly IMongoCollection<UserDTO> _db;

        public UserRepository(ILogger<UserRepository> logger, IConfiguration configuration, MongoDBContext db)
        {
            _logger = logger;
            _configuration = configuration;
            _db = db.GetCollection<UserDTO>("Users"); //Fortæller at vores added-informationer(fx. nye users) kommer inde under Collection "Users" på Mongo

        }


        public IEnumerable<UserDTO> GetAllUsers()
        {
            return _db.Find(_ => true).ToList();
        }

        public UserDTO GetUser(int id)
        {
            // Use MongoDB's LINQ methods to query for a user by ID
            return _db.Find(u => u.UserId == id).FirstOrDefault();
        }

        public void AddUser(UserDTO user)
        {
            // Insert a new user document into the collection
            _db.InsertOne(user);
        }

        public void UpdateUser(UserDTO user)
        {
            // Update an existing user document based on their ID
            var filter = Builders<UserDTO>.Filter
                                          .Eq(u => u.UserId, user.UserId);
            _db.ReplaceOne(filter, user);
        }

        public void DeleteUser(int id)
        {
            // Delete a user document by ID
            var filter = Builders<UserDTO>.Filter.Eq(u => u.UserId, id);
            _db.DeleteOne(filter);
        }
    }
}
