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
            _db = db.GetCollection<UserDTO>("Users");
        }

        public IEnumerable<UserDTO> GetAllUsers()
        {
            _logger.LogInformation("Fetching all users from the database");
            return _db.Find(_ => true).ToList();
        }

        public UserDTO GetUser(int id)
        {
            _logger.LogInformation($"Fetching user with ID: {id}");
            var user = _db.Find(u => u.UserId == id).FirstOrDefault();
            if (user == null)
            {
                _logger.LogWarning($"User with ID: {id} not found");
            }
            return user;
        }

        public UserDTO GetUserByName(string username)
        {
            _logger.LogInformation($"Fetching user with username: {username}");
            var user = _db.Find(u => u.Username == username).FirstOrDefault();
            if (user == null)
            {
                _logger.LogWarning($"User with username: {username} not found");
            }
            return user;
        }

        public void AddUser(UserDTO user)
        {
            _logger.LogInformation($"Adding new user with username: {user.Username}");
            _db.InsertOne(user);
            _logger.LogInformation("User added successfully");
        }

        public void UpdateUser(UserDTO user)
        {
            _logger.LogInformation($"Updating user with ID: {user.UserId}");
            var filter = Builders<UserDTO>.Filter.Eq(u => u.UserId, user.UserId);
            _db.ReplaceOne(filter, user);
            _logger.LogInformation("User updated successfully");
        }

        public void DeleteUser(int id)
        {
            _logger.LogInformation($"Deleting user with ID: {id}");
            var filter = Builders<UserDTO>.Filter.Eq(u => u.UserId, id);
            _db.DeleteOne(filter);
            _logger.LogInformation("User deleted successfully");
        }
    }
}
